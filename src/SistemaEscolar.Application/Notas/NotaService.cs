using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;

namespace SistemaEscolar.Application.Notas;

public sealed class NotaService : INotaService
{
    private readonly INotaRepository _notaRepository;
    private readonly IAlunoService _alunoService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IPeriodoService _periodoService;
    private readonly IProfessorService _professorService;

    public NotaService(
        INotaRepository notaRepository,
        IAlunoService alunoService,
        ICurrentUserService currentUserService,
        IDisciplinaService disciplinaService,
        IPeriodoService periodoService,
        IProfessorService professorService)
    {
        _notaRepository = notaRepository;
        _alunoService = alunoService;
        _currentUserService = currentUserService;
        _disciplinaService = disciplinaService;
        _periodoService = periodoService;
        _professorService = professorService;
    }

    public async Task<IReadOnlyList<NotaListItemDto>> ListarAsync(NotaListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var notas = await _notaRepository.GetAllAsync(filter, cancellationToken);

        if (IsProfessorOnly())
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(_currentUserService.UserName, cancellationToken);
            if (escopo is not null)
            {
                notas = notas.Where(x => x.ProfessorId == escopo.ProfessorId).ToList();
            }
        }

        return notas;
    }

    public async Task<NotaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var nota = await _notaRepository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return null;
        }

        var alunos = await _alunoService.ListarAsync(null, cancellationToken);
        var disciplinas = await _disciplinaService.ListarAsync(null, cancellationToken);
        var professores = await _professorService.ListarAsync(null, cancellationToken);
        var periodo = await _periodoService.ObterPorIdAsync(nota.PeriodoLancamentoId, cancellationToken);

        var alunoNome = alunos.FirstOrDefault(x => x.Id == nota.AlunoId)?.NomeCompleto ?? "Aluno não encontrado";
        var disciplinaNome = disciplinas.FirstOrDefault(x => x.Id == nota.DisciplinaId)?.Nome ?? "Disciplina não encontrada";
        var professorNome = professores.FirstOrDefault(x => x.Id == nota.ProfessorId)?.NomeCompleto ?? "Professor não encontrado";

        return new NotaListItemDto(
            nota.Id,
            nota.AlunoId,
            alunoNome,
            nota.DisciplinaId,
            disciplinaNome,
            nota.ProfessorId,
            professorNome,
            nota.PeriodoLancamentoId,
            periodo?.Descricao ?? "Período não encontrado",
            periodo?.AnoLetivo ?? 0,
            periodo?.Trimestre ?? 0,
            nota.Avaliacao1,
            nota.Avaliacao2,
            nota.Avaliacao3,
            nota.RecuperacaoParalela,
            nota.ResultadoUnidade,
            nota.ResultadoFinalUnidade,
            nota.IsFinalizada);
    }

    public async Task<NotaCreateResult> CriarAsync(NotaCreateRequest request, CancellationToken cancellationToken = default)
    {
        var escopo = await AplicarEscopoDoProfessorAsync(request, cancellationToken);
        if (!escopo.Result.Succeeded)
        {
            return escopo.Result;
        }

        request = escopo.Request;

        var validation = await ValidateRequestAsync(request, null, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var resultadoUnidade = CalcularResultadoUnidade(request.Avaliacao1, request.Avaliacao2, request.Avaliacao3);
        var resultadoFinalUnidade = request.RecuperacaoParalela.HasValue
            ? Math.Round(Math.Max(resultadoUnidade, request.RecuperacaoParalela.Value), 2)
            : resultadoUnidade;

        var nota = new Domain.Entities.Nota
        {
            Id = Guid.NewGuid(),
            AlunoId = request.AlunoId,
            DisciplinaId = request.DisciplinaId,
            ProfessorId = request.ProfessorId,
            PeriodoLancamentoId = request.PeriodoLancamentoId,
            Avaliacao1 = request.Avaliacao1,
            Avaliacao2 = request.Avaliacao2,
            Avaliacao3 = request.Avaliacao3,
            RecuperacaoParalela = request.RecuperacaoParalela,
            ResultadoUnidade = resultadoUnidade,
            ResultadoFinalUnidade = resultadoFinalUnidade,
            Valor = resultadoFinalUnidade,
            IsFinalizada = request.IsFinalizada
        };

        await _notaRepository.AddAsync(nota, cancellationToken);
        return NotaCreateResult.Success();
    }

    public async Task<NotaCreateResult> AtualizarAsync(Guid id, NotaCreateRequest request, CancellationToken cancellationToken = default)
    {
        var nota = await _notaRepository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return NotaCreateResult.Fail("Lançamento de nota não encontrado.");
        }

        var escopo = await AplicarEscopoDoProfessorAsync(request, cancellationToken);
        if (!escopo.Result.Succeeded)
        {
            return escopo.Result;
        }

        request = escopo.Request;

        var validation = await ValidateRequestAsync(request, id, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var resultadoUnidade = CalcularResultadoUnidade(request.Avaliacao1, request.Avaliacao2, request.Avaliacao3);
        var resultadoFinalUnidade = request.RecuperacaoParalela.HasValue
            ? Math.Round(Math.Max(resultadoUnidade, request.RecuperacaoParalela.Value), 2)
            : resultadoUnidade;

        nota.AlunoId = request.AlunoId;
        nota.DisciplinaId = request.DisciplinaId;
        nota.ProfessorId = request.ProfessorId;
        nota.PeriodoLancamentoId = request.PeriodoLancamentoId;
        nota.Avaliacao1 = request.Avaliacao1;
        nota.Avaliacao2 = request.Avaliacao2;
        nota.Avaliacao3 = request.Avaliacao3;
        nota.RecuperacaoParalela = request.RecuperacaoParalela;
        nota.ResultadoUnidade = resultadoUnidade;
        nota.ResultadoFinalUnidade = resultadoFinalUnidade;
        nota.Valor = resultadoFinalUnidade;
        nota.IsFinalizada = request.IsFinalizada;

        await _notaRepository.UpdateAsync(nota, cancellationToken);
        return NotaCreateResult.Success();
    }

    public async Task<bool> AlternarFinalizacaoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var nota = await _notaRepository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return false;
        }

        var periodo = await _periodoService.ObterPorIdAsync(nota.PeriodoLancamentoId, cancellationToken);
        if (periodo is not null && !periodo.IsAberto && !_currentUserService.IsInRole("Diretor"))
        {
            return false;
        }

        nota.IsFinalizada = !nota.IsFinalizada;
        await _notaRepository.UpdateAsync(nota, cancellationToken);
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var nota = await _notaRepository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return false;
        }

        var periodo = await _periodoService.ObterPorIdAsync(nota.PeriodoLancamentoId, cancellationToken);
        if (periodo is not null && !periodo.IsAberto && !_currentUserService.IsInRole("Diretor"))
        {
            return false;
        }

        await _notaRepository.SoftDeleteAsync(nota, cancellationToken);
        return true;
    }

    private async Task<NotaCreateResult> ValidateRequestAsync(NotaCreateRequest request, Guid? ignoreId, CancellationToken cancellationToken)
    {
        if (request.AlunoId == Guid.Empty)
        {
            return NotaCreateResult.Fail("Selecione um aluno válido.");
        }

        if (request.DisciplinaId == Guid.Empty)
        {
            return NotaCreateResult.Fail("Selecione uma disciplina válida.");
        }

        if (request.ProfessorId == Guid.Empty)
        {
            return NotaCreateResult.Fail("Selecione um professor válido.");
        }

        if (request.PeriodoLancamentoId == Guid.Empty)
        {
            return NotaCreateResult.Fail("Selecione um período de lançamento válido.");
        }

        if (!request.Avaliacao1.HasValue
            && !request.Avaliacao2.HasValue
            && !request.Avaliacao3.HasValue)
        {
            return NotaCreateResult.Fail("Informe ao menos uma avaliação para o período.");
        }

        if ((request.Avaliacao1.HasValue && !IsNotaValida(request.Avaliacao1.Value))
            || (request.Avaliacao2.HasValue && !IsNotaValida(request.Avaliacao2.Value))
            || (request.Avaliacao3.HasValue && !IsNotaValida(request.Avaliacao3.Value))
            || (request.RecuperacaoParalela.HasValue && !IsNotaValida(request.RecuperacaoParalela.Value)))
        {
            return NotaCreateResult.Fail("As notas devem estar entre 0 e 10.");
        }

        var periodo = await _periodoService.ObterPorIdAsync(request.PeriodoLancamentoId, cancellationToken);
        if (periodo is null)
        {
            return NotaCreateResult.Fail("Não há período de lançamento configurado com este identificador.");
        }

        if (!periodo.IsAberto && !_currentUserService.IsInRole("Diretor"))
        {
            return NotaCreateResult.Fail("Período fechado impede alterações de notas para este perfil.");
        }

        var alunos = await _alunoService.ListarAsync(null, cancellationToken);
        var aluno = alunos.FirstOrDefault(x => x.Id == request.AlunoId);
        if (aluno is null)
        {
            return NotaCreateResult.Fail("Aluno não encontrado.");
        }

        if (!aluno.IsAtivo)
        {
            return NotaCreateResult.Fail("Aluno inativo não pode receber lançamento de nota.");
        }

        var disciplinas = await _disciplinaService.ListarAsync(null, cancellationToken);
        if (disciplinas.All(x => x.Id != request.DisciplinaId))
        {
            return NotaCreateResult.Fail("Disciplina não encontrada.");
        }

        var professores = await _professorService.ListarAsync(null, cancellationToken);
        if (professores.All(x => x.Id != request.ProfessorId))
        {
            return NotaCreateResult.Fail("Professor não encontrado.");
        }

        if (await _notaRepository.LancamentoDuplicadoExisteAsync(
            request.AlunoId,
            request.DisciplinaId,
            request.PeriodoLancamentoId,
            ignoreId,
            cancellationToken))
        {
            return NotaCreateResult.Fail("Já existe lançamento para este aluno, disciplina e período.");
        }

        return NotaCreateResult.Success();
    }

    private bool IsProfessorOnly() =>
        _currentUserService.IsInRole("Professor")
        && !_currentUserService.IsInRole("Diretor")
        && !_currentUserService.IsInRole("Coordenador")
        && !_currentUserService.IsInRole("Cordenador")
        && !_currentUserService.IsInRole("Secretaria");

    private async Task<(NotaCreateResult Result, NotaCreateRequest Request)> AplicarEscopoDoProfessorAsync(NotaCreateRequest request, CancellationToken cancellationToken)
    {
        if (!IsProfessorOnly())
        {
            return (NotaCreateResult.Success(), request);
        }

        var escopo = await _professorService.ObterEscopoPorUsuarioAsync(_currentUserService.UserName, cancellationToken);
        if (escopo is null)
        {
            return (NotaCreateResult.Fail("Professor não encontrado para o usuário atual."), request);
        }

        request = request with { ProfessorId = escopo.ProfessorId };

        if (!escopo.DisciplinaIds.Contains(request.DisciplinaId))
        {
            return (NotaCreateResult.Fail("Você não está vinculado a esta disciplina."), request);
        }

        var aluno = await _alunoService.ObterPorIdAsync(request.AlunoId, cancellationToken);
        if (aluno is null || aluno.Turma is null || !escopo.TurmaNomes.Contains(aluno.Turma, StringComparer.OrdinalIgnoreCase))
        {
            return (NotaCreateResult.Fail("Você não está vinculado à turma deste aluno."), request);
        }

        return (NotaCreateResult.Success(), request);
    }

    private static bool IsNotaValida(decimal valor) => valor >= 0m && valor <= 10m;

    private static decimal CalcularResultadoUnidade(decimal? avaliacao1, decimal? avaliacao2, decimal? avaliacao3)
    {
        var avaliacoes = new[] { avaliacao1, avaliacao2, avaliacao3 }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        return avaliacoes.Count == 0
            ? 0m
            : Math.Round(avaliacoes.Average(), 2);
    }
}
