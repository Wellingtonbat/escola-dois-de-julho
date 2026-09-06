using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Application.Alunos;

public sealed class AlunoService : IAlunoService
{
    private readonly IAlunoRepository _alunoRepository;
    private readonly ISerieRepository _serieRepository;
    private readonly ITurmaRepository _turmaRepository;

    public AlunoService(IAlunoRepository alunoRepository, ISerieRepository serieRepository, ITurmaRepository turmaRepository)
    {
        _alunoRepository = alunoRepository;
        _serieRepository = serieRepository;
        _turmaRepository = turmaRepository;
    }

    public async Task<IReadOnlyList<AlunoListItemDto>> ListarAsync(AlunoListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var alunos = await _alunoRepository.GetAllAsync(filter, cancellationToken);
        var series = await _serieRepository.GetAllAsync(null, cancellationToken);
        var nomePorSerieId = series.ToDictionary(x => x.Id, x => x.Nome);
        var turmas = await _turmaRepository.GetAllAsync(null, cancellationToken);
        var nomePorTurmaId = turmas.ToDictionary(x => x.Id, x => x.Nome);

        return alunos
            .OrderBy(x => x.NomeCompleto)
            .Select(x => new AlunoListItemDto(
                x.Id,
                x.Matricula,
                x.Cpf,
                x.NomeCompleto,
                x.DataNascimento,
                x.AnoLetivo,
                x.SerieId,
                nomePorSerieId.TryGetValue(x.SerieId, out var nome) ? nome : "Série não encontrada",
                x.TurmaId,
                x.TurmaId.HasValue ? (nomePorTurmaId.TryGetValue(x.TurmaId.Value, out var turmaNome) ? turmaNome : "Turma não encontrada") : null,
                x.IsAtivo))
            .ToList();
    }

    public async Task<AlunoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id, cancellationToken);
        if (aluno is null)
        {
            return null;
        }

        var serie = await _serieRepository.GetByIdAsync(aluno.SerieId, cancellationToken);
        var turma = aluno.TurmaId.HasValue ? await _turmaRepository.GetByIdAsync(aluno.TurmaId.Value, cancellationToken) : null;

        return new AlunoListItemDto(
            aluno.Id,
            aluno.Matricula,
            aluno.Cpf,
            aluno.NomeCompleto,
            aluno.DataNascimento,
            aluno.AnoLetivo,
            aluno.SerieId,
            serie?.Nome ?? "Série não encontrada",
            aluno.TurmaId,
            aluno.TurmaId.HasValue ? (turma?.Nome ?? "Turma não encontrada") : null,
            aluno.IsAtivo);
    }

    public async Task<AlunoCreateResult> CriarAsync(AlunoCreateRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (!validation.Result.Succeeded)
        {
            return validation.Result;
        }

        var matricula = await _alunoRepository.ObterMatriculaPorCpfAsync(validation.Cpf, cancellationToken)
            ?? await _alunoRepository.ProximaMatriculaAsync(cancellationToken);

        var aluno = new Domain.Entities.Aluno
        {
            Id = Guid.NewGuid(),
            Matricula = matricula,
            Cpf = validation.Cpf,
            NomeCompleto = validation.NomeCompleto,
            DataNascimento = request.DataNascimento.Date,
            AnoLetivo = request.AnoLetivo,
            SerieId = request.SerieId,
            TurmaId = request.TurmaId,
            IsAtivo = request.IsAtivo
        };

        await _alunoRepository.AddAsync(aluno, cancellationToken);

        return AlunoCreateResult.Success();
    }

    public async Task<AlunoCreateResult> AtualizarAsync(Guid id, AlunoCreateRequest request, CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id, cancellationToken);
        if (aluno is null)
        {
            return AlunoCreateResult.Fail("Aluno não encontrado.");
        }

        var validation = await ValidateAsync(request, id, cancellationToken);
        if (!validation.Result.Succeeded)
        {
            return validation.Result;
        }

        aluno.Cpf = validation.Cpf;
        aluno.NomeCompleto = validation.NomeCompleto;
        aluno.DataNascimento = request.DataNascimento.Date;
        aluno.AnoLetivo = request.AnoLetivo;
        aluno.SerieId = request.SerieId;
        aluno.TurmaId = request.TurmaId;
        aluno.IsAtivo = request.IsAtivo;

        await _alunoRepository.UpdateAsync(aluno, cancellationToken);

        return AlunoCreateResult.Success();
    }

    public async Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id, cancellationToken);
        if (aluno is null)
        {
            return false;
        }

        aluno.IsAtivo = !aluno.IsAtivo;
        await _alunoRepository.UpdateAsync(aluno, cancellationToken);

        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id, cancellationToken);
        if (aluno is null)
        {
            return false;
        }

        await _alunoRepository.SoftDeleteAsync(aluno, cancellationToken);
        return true;
    }

    private async Task<(AlunoCreateResult Result, string Cpf, string NomeCompleto)> ValidateAsync(
        AlunoCreateRequest request,
        Guid? ignoreId,
        CancellationToken cancellationToken)
    {
        var cpf = NormalizeCpf(request.Cpf);
        var nomeCompleto = (request.NomeCompleto ?? string.Empty).Trim();

        if (cpf.Length != 11)
        {
            return (AlunoCreateResult.Fail("Informe um CPF válido com 11 dígitos."), cpf, nomeCompleto);
        }

        if (string.IsNullOrWhiteSpace(nomeCompleto))
        {
            return (AlunoCreateResult.Fail("O nome do aluno é obrigatório."), cpf, nomeCompleto);
        }

        if (request.DataNascimento == default || request.DataNascimento > DateTime.UtcNow.Date)
        {
            return (AlunoCreateResult.Fail("A data de nascimento é obrigatória e deve ser válida."), cpf, nomeCompleto);
        }

        if (request.AnoLetivo < 2000 || request.AnoLetivo > 2100)
        {
            return (AlunoCreateResult.Fail("Informe um ano letivo válido."), cpf, nomeCompleto);
        }

        var serie = await _serieRepository.GetByIdAsync(request.SerieId, cancellationToken);
        if (serie is null)
        {
            return (AlunoCreateResult.Fail("Série não encontrada."), cpf, nomeCompleto);
        }

        if (request.TurmaId.HasValue)
        {
            var turma = await _turmaRepository.GetByIdAsync(request.TurmaId.Value, cancellationToken);
            if (turma is null)
            {
                return (AlunoCreateResult.Fail("Turma não encontrada."), cpf, nomeCompleto);
            }

            if (turma.SerieId != request.SerieId)
            {
                return (AlunoCreateResult.Fail("A turma selecionada não pertence à série informada."), cpf, nomeCompleto);
            }
        }

        if (await _alunoRepository.CpfMatriculadoNoAnoAsync(cpf, request.AnoLetivo, ignoreId, cancellationToken))
        {
            return (AlunoCreateResult.Fail("Já existe matrícula para este CPF no ano letivo informado."), cpf, nomeCompleto);
        }

        var maiorOrdemAnterior = await _alunoRepository.ObterMaiorOrdemSerieAnteriorAsync(cpf, request.AnoLetivo, cancellationToken);
        if (maiorOrdemAnterior.HasValue && serie.Ordem < maiorOrdemAnterior.Value)
        {
            return (AlunoCreateResult.Fail("Não é possível matricular o aluno em série anterior a uma já cursada em ano letivo anterior."), cpf, nomeCompleto);
        }

        return (AlunoCreateResult.Success(), cpf, nomeCompleto);
    }

    private static string NormalizeCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        return new string(cpf.Where(char.IsDigit).ToArray());
    }
}
