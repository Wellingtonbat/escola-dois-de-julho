using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Notas;

namespace SistemaEscolar.Application.Resultados;

public sealed class RecuperacaoFinalService : IRecuperacaoFinalService
{
    private const decimal MediaAprovacao = 5.0m;
    private const int BimestresEsperados = 3;

    private readonly IRecuperacaoFinalRepository _recuperacaoFinalRepository;
    private readonly INotaService _notaService;
    private readonly IAlunoService _alunoService;
    private readonly ICurrentUserService _currentUserService;

    public RecuperacaoFinalService(
        IRecuperacaoFinalRepository recuperacaoFinalRepository,
        INotaService notaService,
        IAlunoService alunoService,
        ICurrentUserService currentUserService)
    {
        _recuperacaoFinalRepository = recuperacaoFinalRepository;
        _notaService = notaService;
        _alunoService = alunoService;
        _currentUserService = currentUserService;
    }

    public async Task<RecuperacaoFinalSaveResult> SalvarAsync(RecuperacaoFinalSaveRequest request, CancellationToken cancellationToken = default)
    {
        // A Avaliação Final (nome usado na escola para a Recuperação Final anual) é lançada só pela Diretoria.
        if (!PermissoesPerfil.PodeLancarAvaliacaoFinal(_currentUserService.IsInRole))
        {
            return RecuperacaoFinalSaveResult.Fail("Somente Diretor ou Vice-Diretor podem lançar a Avaliação Final.");
        }

        if (request.Valor < 0m || request.Valor > 10m)
        {
            return RecuperacaoFinalSaveResult.Fail("A nota deve estar entre 0 e 10.");
        }

        var aluno = await _alunoService.ObterPorIdAsync(request.AlunoId, cancellationToken);
        if (aluno is null)
        {
            return RecuperacaoFinalSaveResult.Fail("Aluno não encontrado.");
        }

        var notasDoAno = (await _notaService.ListarAsync(null, cancellationToken))
            .Where(x => x.AlunoId == request.AlunoId && x.DisciplinaId == request.DisciplinaId && x.AnoLetivo == request.AnoLetivo)
            .ToList();

        if (notasDoAno.Count < BimestresEsperados)
        {
            return RecuperacaoFinalSaveResult.Fail("A Avaliação Final só pode ser lançada após o lançamento dos 3 trimestres.");
        }

        var mediaAnual = Math.Round(notasDoAno.Average(x => x.ResultadoFinalUnidade), 2);
        if (mediaAnual >= MediaAprovacao)
        {
            return RecuperacaoFinalSaveResult.Fail("A Avaliação Final só pode ser lançada quando a média anual for menor que 5,0.");
        }

        await _recuperacaoFinalRepository.SalvarAsync(request.AlunoId, request.DisciplinaId, request.AnoLetivo, request.Valor, cancellationToken);
        return RecuperacaoFinalSaveResult.Success();
    }

    public Task<IReadOnlyDictionary<Guid, decimal>> ListarPorAlunoEAnoAsync(Guid alunoId, int anoLetivo, CancellationToken cancellationToken = default) =>
        _recuperacaoFinalRepository.ListarPorAlunoEAnoAsync(alunoId, anoLetivo, cancellationToken);

    public Task<IReadOnlyDictionary<(Guid AlunoId, Guid DisciplinaId), decimal>> ListarPorAnoAsync(int anoLetivo, CancellationToken cancellationToken = default) =>
        _recuperacaoFinalRepository.ListarPorAnoAsync(anoLetivo, cancellationToken);
}
