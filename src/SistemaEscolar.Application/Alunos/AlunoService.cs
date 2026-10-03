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
                x.MatriculaPrefeitura,
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
            aluno.MatriculaPrefeitura,
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
            MatriculaPrefeitura = validation.MatriculaPrefeitura,
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
        aluno.MatriculaPrefeitura = validation.MatriculaPrefeitura;
        aluno.NomeCompleto = validation.NomeCompleto;
        aluno.DataNascimento = request.DataNascimento.Date;
        aluno.AnoLetivo = request.AnoLetivo;
        aluno.SerieId = request.SerieId;
        aluno.TurmaId = request.TurmaId;
        aluno.IsAtivo = request.IsAtivo;

        await _alunoRepository.UpdateAsync(aluno, cancellationToken);

        return AlunoCreateResult.Success();
    }

    // Usado pela tela de Ata, que também permite preencher/corrigir a matrícula da prefeitura
    // diretamente na grade, sem passar pelo formulário completo do aluno.
    public async Task<AlunoCreateResult> AtualizarMatriculaPrefeituraAsync(
        Guid id, string? matriculaPrefeitura, CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id, cancellationToken);
        if (aluno is null)
        {
            return AlunoCreateResult.Fail("Aluno não encontrado.");
        }

        var validacao = await ValidarMatriculaPrefeituraAsync(matriculaPrefeitura, id, cancellationToken);
        if (!validacao.Result.Succeeded)
        {
            return validacao.Result;
        }

        aluno.MatriculaPrefeitura = validacao.Valor;
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

    private async Task<(AlunoCreateResult Result, string Cpf, string NomeCompleto, string? MatriculaPrefeitura)> ValidateAsync(
        AlunoCreateRequest request,
        Guid? ignoreId,
        CancellationToken cancellationToken)
    {
        var cpf = NormalizeCpf(request.Cpf);
        var nomeCompleto = (request.NomeCompleto ?? string.Empty).Trim();

        if (cpf.Length != 11)
        {
            return (AlunoCreateResult.Fail("Informe um CPF válido com 11 dígitos."), cpf, nomeCompleto, null);
        }

        if (string.IsNullOrWhiteSpace(nomeCompleto))
        {
            return (AlunoCreateResult.Fail("O nome do aluno é obrigatório."), cpf, nomeCompleto, null);
        }

        if (request.DataNascimento == default || request.DataNascimento > DateTime.UtcNow.Date)
        {
            return (AlunoCreateResult.Fail("A data de nascimento é obrigatória e deve ser válida."), cpf, nomeCompleto, null);
        }

        if (request.AnoLetivo < 2000 || request.AnoLetivo > 2100)
        {
            return (AlunoCreateResult.Fail("Informe um ano letivo válido."), cpf, nomeCompleto, null);
        }

        var serie = await _serieRepository.GetByIdAsync(request.SerieId, cancellationToken);
        if (serie is null)
        {
            return (AlunoCreateResult.Fail("Série não encontrada."), cpf, nomeCompleto, null);
        }

        if (request.TurmaId.HasValue)
        {
            var turma = await _turmaRepository.GetByIdAsync(request.TurmaId.Value, cancellationToken);
            if (turma is null)
            {
                return (AlunoCreateResult.Fail("Turma não encontrada."), cpf, nomeCompleto, null);
            }

            if (turma.SerieId != request.SerieId)
            {
                return (AlunoCreateResult.Fail("A turma selecionada não pertence à série informada."), cpf, nomeCompleto, null);
            }
        }

        if (await _alunoRepository.CpfMatriculadoNoAnoAsync(cpf, request.AnoLetivo, ignoreId, cancellationToken))
        {
            return (AlunoCreateResult.Fail("Já existe matrícula para este CPF no ano letivo informado."), cpf, nomeCompleto, null);
        }

        var maiorOrdemAnterior = await _alunoRepository.ObterMaiorOrdemSerieAnteriorAsync(cpf, request.AnoLetivo, cancellationToken);
        if (maiorOrdemAnterior.HasValue && serie.Ordem < maiorOrdemAnterior.Value)
        {
            return (AlunoCreateResult.Fail("Não é possível matricular o aluno em série anterior a uma já cursada em ano letivo anterior."), cpf, nomeCompleto, null);
        }

        var matricula = await ValidarMatriculaPrefeituraAsync(request.MatriculaPrefeitura, ignoreId, cancellationToken);
        if (!matricula.Result.Succeeded)
        {
            return (matricula.Result, cpf, nomeCompleto, null);
        }

        return (AlunoCreateResult.Success(), cpf, nomeCompleto, matricula.Valor);
    }

    // Em branco é válido (o campo é opcional — nem toda escola tem o número da prefeitura ainda);
    // quando preenchido, precisa ser único entre os alunos não excluídos.
    private async Task<(AlunoCreateResult Result, string? Valor)> ValidarMatriculaPrefeituraAsync(
        string? matriculaPrefeitura, Guid? ignoreId, CancellationToken cancellationToken)
    {
        var valor = string.IsNullOrWhiteSpace(matriculaPrefeitura) ? null : matriculaPrefeitura.Trim();
        if (valor is null)
        {
            return (AlunoCreateResult.Success(), null);
        }

        if (await _alunoRepository.MatriculaPrefeituraExisteAsync(valor, ignoreId, cancellationToken))
        {
            return (AlunoCreateResult.Fail($"Já existe um aluno cadastrado com a matrícula da prefeitura \"{valor}\"."), valor);
        }

        return (AlunoCreateResult.Success(), valor);
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
