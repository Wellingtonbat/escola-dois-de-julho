using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Professores;

namespace SistemaEscolar.Application.Resultados;

public sealed class RecuperacaoFinalService : IRecuperacaoFinalService
{
    private const decimal MediaAprovacao = 5.0m;
    private const int BimestresEsperados = 3;

    private readonly IRecuperacaoFinalRepository _recuperacaoFinalRepository;
    private readonly INotaService _notaService;
    private readonly IAlunoService _alunoService;
    private readonly IProfessorService _professorService;
    private readonly ICurrentUserService _currentUserService;

    public RecuperacaoFinalService(
        IRecuperacaoFinalRepository recuperacaoFinalRepository,
        INotaService notaService,
        IAlunoService alunoService,
        IProfessorService professorService,
        ICurrentUserService currentUserService)
    {
        _recuperacaoFinalRepository = recuperacaoFinalRepository;
        _notaService = notaService;
        _alunoService = alunoService;
        _professorService = professorService;
        _currentUserService = currentUserService;
    }

    public async Task<RecuperacaoFinalSaveResult> SalvarAsync(RecuperacaoFinalSaveRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Valor < 0m || request.Valor > 10m)
        {
            return RecuperacaoFinalSaveResult.Fail("A nota deve estar entre 0 e 10.");
        }

        var aluno = await _alunoService.ObterPorIdAsync(request.AlunoId, cancellationToken);
        if (aluno is null)
        {
            return RecuperacaoFinalSaveResult.Fail("Aluno não encontrado.");
        }

        if (IsProfessorOnly())
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(_currentUserService.UserName, cancellationToken);
            if (escopo is null || !escopo.DisciplinaIds.Contains(request.DisciplinaId)
                || !aluno.TurmaId.HasValue || !escopo.TurmaIds.Contains(aluno.TurmaId.Value))
            {
                return RecuperacaoFinalSaveResult.Fail("Você não está vinculado à turma ou disciplina deste aluno.");
            }
        }

        var notasDoAno = (await _notaService.ListarAsync(null, cancellationToken))
            .Where(x => x.AlunoId == request.AlunoId && x.DisciplinaId == request.DisciplinaId && x.AnoLetivo == request.AnoLetivo)
            .ToList();

        if (notasDoAno.Count < BimestresEsperados)
        {
            return RecuperacaoFinalSaveResult.Fail("A Recuperação Final só pode ser lançada após o lançamento dos 3 trimestres.");
        }

        var mediaAnual = Math.Round(notasDoAno.Average(x => x.ResultadoFinalUnidade), 2);
        if (mediaAnual >= MediaAprovacao)
        {
            return RecuperacaoFinalSaveResult.Fail("A Recuperação Final só pode ser lançada quando a média anual for menor que 5,0.");
        }

        await _recuperacaoFinalRepository.SalvarAsync(request.AlunoId, request.DisciplinaId, request.AnoLetivo, request.Valor, cancellationToken);
        return RecuperacaoFinalSaveResult.Success();
    }

    private bool IsProfessorOnly() =>
        _currentUserService.IsInRole("Professor")
        && !_currentUserService.IsInRole("Diretor")
        && !_currentUserService.IsInRole("Coordenador")
        && !_currentUserService.IsInRole("Cordenador")
        && !_currentUserService.IsInRole("Secretaria");
}
