using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Application.Professores;

public sealed class ProfessorService : IProfessorService
{
    private readonly IProfessorRepository _professorRepository;
    private readonly IProfessorAtribuicaoRepository _atribuicaoRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly ISerieRepository _serieRepository;
    private readonly IAccountProvisioner _accountProvisioner;

    public ProfessorService(
        IProfessorRepository professorRepository,
        IProfessorAtribuicaoRepository atribuicaoRepository,
        ITurmaRepository turmaRepository,
        IDisciplinaRepository disciplinaRepository,
        ISerieRepository serieRepository,
        IAccountProvisioner accountProvisioner)
    {
        _professorRepository = professorRepository;
        _atribuicaoRepository = atribuicaoRepository;
        _turmaRepository = turmaRepository;
        _disciplinaRepository = disciplinaRepository;
        _serieRepository = serieRepository;
        _accountProvisioner = accountProvisioner;
    }

    public async Task<IReadOnlyList<ProfessorListItemDto>> ListarAsync(ProfessorListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var professores = await _professorRepository.GetAllAsync(filter, cancellationToken);
        var todasAtribuicoes = await _atribuicaoRepository.GetAllAsync(cancellationToken);
        var (turmas, disciplinas, series) = await CarregarTurmasDisciplinasESeriesAsync(cancellationToken);

        return professores
            .OrderBy(x => x.NomeCompleto)
            .Select(professor =>
            {
                var atribuicoesDto = todasAtribuicoes
                    .Where(a => a.ProfessorId == professor.Id)
                    .Select(a => MontarAtribuicaoDto(a.TurmaId, a.DisciplinaId, turmas, disciplinas, series))
                    .ToList();

                return new ProfessorListItemDto(professor.Id, professor.NomeCompleto, professor.Email, professor.UsuarioCpf, atribuicoesDto, professor.IsAtivo);
            })
            .ToList();
    }

    public async Task<ProfessorListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var professor = await _professorRepository.GetByIdAsync(id, cancellationToken);
        if (professor is null)
        {
            return null;
        }

        var atribuicoes = await _atribuicaoRepository.GetByProfessorIdAsync(id, cancellationToken);
        var (turmas, disciplinas, series) = await CarregarTurmasDisciplinasESeriesAsync(cancellationToken);

        var atribuicoesDto = atribuicoes
            .Select(a => MontarAtribuicaoDto(a.TurmaId, a.DisciplinaId, turmas, disciplinas, series))
            .ToList();

        return new ProfessorListItemDto(professor.Id, professor.NomeCompleto, professor.Email, professor.UsuarioCpf, atribuicoesDto, professor.IsAtivo);
    }

    public async Task<ProfessorCreateResult> CriarAsync(ProfessorCreateRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (!validation.Result.Succeeded)
        {
            return validation.Result;
        }

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            return ProfessorCreateResult.Fail("Defina a senha padrão do professor.");
        }

        var contaResult = await _accountProvisioner.CriarOuReiniciarContaAsync(
            validation.UsuarioCpf, validation.NomeCompleto, validation.Email, request.Senha, "Professor", cancellationToken);

        if (!contaResult.Succeeded)
        {
            return ProfessorCreateResult.Fail(contaResult.ErrorMessage ?? "Não foi possível criar o acesso do professor.");
        }

        var professor = new Domain.Entities.Professor
        {
            Id = Guid.NewGuid(),
            NomeCompleto = validation.NomeCompleto,
            Email = validation.Email,
            UsuarioCpf = validation.UsuarioCpf,
            IsAtivo = request.IsAtivo
        };

        await _professorRepository.AddAsync(professor, cancellationToken);
        await _atribuicaoRepository.SubstituirAsync(professor.Id, validation.AtribuicoesValidas, cancellationToken);

        return ProfessorCreateResult.Success();
    }

    public async Task<ProfessorCreateResult> AtualizarAsync(Guid id, ProfessorCreateRequest request, CancellationToken cancellationToken = default)
    {
        var professor = await _professorRepository.GetByIdAsync(id, cancellationToken);
        if (professor is null)
        {
            return ProfessorCreateResult.Fail("Professor não encontrado.");
        }

        var validation = await ValidateAsync(request, id, cancellationToken);
        if (!validation.Result.Succeeded)
        {
            return validation.Result;
        }

        professor.NomeCompleto = validation.NomeCompleto;
        professor.Email = validation.Email;
        professor.UsuarioCpf = validation.UsuarioCpf;
        professor.IsAtivo = request.IsAtivo;

        await _professorRepository.UpdateAsync(professor, cancellationToken);
        await _atribuicaoRepository.SubstituirAsync(professor.Id, validation.AtribuicoesValidas, cancellationToken);

        return ProfessorCreateResult.Success();
    }

    public async Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var professor = await _professorRepository.GetByIdAsync(id, cancellationToken);
        if (professor is null)
        {
            return false;
        }

        professor.IsAtivo = !professor.IsAtivo;
        await _professorRepository.UpdateAsync(professor, cancellationToken);

        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var professor = await _professorRepository.GetByIdAsync(id, cancellationToken);
        if (professor is null)
        {
            return false;
        }

        await _professorRepository.SoftDeleteAsync(professor, cancellationToken);
        return true;
    }

    public async Task<ProfessorCreateResult> RedefinirSenhaAsync(Guid id, string novaSenha, CancellationToken cancellationToken = default)
    {
        var professor = await _professorRepository.GetByIdAsync(id, cancellationToken);
        if (professor is null)
        {
            return ProfessorCreateResult.Fail("Professor não encontrado.");
        }

        if (string.IsNullOrWhiteSpace(novaSenha))
        {
            return ProfessorCreateResult.Fail("Informe a nova senha padrão.");
        }

        var contaResult = await _accountProvisioner.CriarOuReiniciarContaAsync(
            professor.UsuarioCpf, professor.NomeCompleto, professor.Email, novaSenha, "Professor", cancellationToken);

        if (!contaResult.Succeeded)
        {
            return ProfessorCreateResult.Fail(contaResult.ErrorMessage ?? "Não foi possível redefinir a senha do professor.");
        }

        return ProfessorCreateResult.Success();
    }

    public async Task<ProfessorEscopoDto?> ObterEscopoPorUsuarioAsync(string? userName, CancellationToken cancellationToken = default)
    {
        var cpf = NormalizeCpf(userName);
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return null;
        }

        var professor = await _professorRepository.GetByUsuarioCpfAsync(cpf, cancellationToken);
        if (professor is null)
        {
            return null;
        }

        var atribuicoes = await _atribuicaoRepository.GetByProfessorIdAsync(professor.Id, cancellationToken);
        var turmaIds = atribuicoes.Select(a => a.TurmaId).Distinct().ToList();
        var disciplinaIds = atribuicoes.Select(a => a.DisciplinaId).Distinct().ToList();

        var todasAsTurmas = await _turmaRepository.GetAllAsync(null, cancellationToken);
        var turmasDoProfessor = todasAsTurmas.Where(t => turmaIds.Contains(t.Id)).ToList();

        var serieIds = turmasDoProfessor.Select(t => t.SerieId).Distinct().ToList();

        return new ProfessorEscopoDto(professor.Id, turmaIds, disciplinaIds, serieIds);
    }

    private async Task<(ProfessorCreateResult Result, string NomeCompleto, string Email, string UsuarioCpf, List<ProfessorAtribuicaoInput> AtribuicoesValidas)> ValidateAsync(
        ProfessorCreateRequest request,
        Guid? ignoreId,
        CancellationToken cancellationToken)
    {
        var nomeCompleto = (request.NomeCompleto ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var usuarioCpf = NormalizeCpf(request.UsuarioCpf);
        var atribuicoesVazias = new List<ProfessorAtribuicaoInput>();

        if (string.IsNullOrWhiteSpace(nomeCompleto))
        {
            return (ProfessorCreateResult.Fail("O nome do professor é obrigatório."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return (ProfessorCreateResult.Fail("O e-mail é obrigatório."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
        }

        if (string.IsNullOrWhiteSpace(usuarioCpf))
        {
            return (ProfessorCreateResult.Fail("O CPF do usuário associado é obrigatório."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
        }

        if (await _professorRepository.NomeEmailExisteAsync(nomeCompleto, email, ignoreId, cancellationToken))
        {
            return (ProfessorCreateResult.Fail("Já existe professor cadastrado com este nome e e-mail."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
        }

        if (await _professorRepository.UsuarioCpfExisteAsync(usuarioCpf, ignoreId, cancellationToken))
        {
            return (ProfessorCreateResult.Fail("Já existe professor cadastrado para este CPF de usuário."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
        }

        var atribuicoesRecebidas = (request.Atribuicoes ?? Array.Empty<ProfessorAtribuicaoInput>())
            .Where(x => x.TurmaId != Guid.Empty && x.DisciplinaId != Guid.Empty)
            .Distinct()
            .ToList();

        if (atribuicoesRecebidas.Count == 0)
        {
            return (ProfessorCreateResult.Fail("Adicione ao menos uma turma e disciplina para o professor."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
        }

        var turmasEncontradas = new Dictionary<Guid, Domain.Entities.Turma>();
        foreach (var turmaId in atribuicoesRecebidas.Select(x => x.TurmaId).Distinct())
        {
            var turma = await _turmaRepository.GetByIdAsync(turmaId, cancellationToken);
            if (turma is null)
            {
                return (ProfessorCreateResult.Fail("Uma das turmas selecionadas não foi encontrada."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
            }

            turmasEncontradas[turmaId] = turma;
        }

        var disciplinasEncontradas = new Dictionary<Guid, Domain.Entities.Disciplina>();
        foreach (var disciplinaId in atribuicoesRecebidas.Select(x => x.DisciplinaId).Distinct())
        {
            var disciplina = await _disciplinaRepository.GetByIdAsync(disciplinaId, cancellationToken);
            if (disciplina is null)
            {
                return (ProfessorCreateResult.Fail("Uma das disciplinas selecionadas não foi encontrada."), nomeCompleto, email, usuarioCpf, atribuicoesVazias);
            }

            disciplinasEncontradas[disciplinaId] = disciplina;
        }

        // Cada combinação de turma + disciplina só pode ter um professor ativo vinculado por vez.
        var todasAsAtribuicoes = await _atribuicaoRepository.GetAllAsync(cancellationToken);
        foreach (var atribuicao in atribuicoesRecebidas)
        {
            var professorConflitanteId = todasAsAtribuicoes
                .Where(x => x.TurmaId == atribuicao.TurmaId && x.DisciplinaId == atribuicao.DisciplinaId && x.ProfessorId != ignoreId)
                .Select(x => x.ProfessorId)
                .Distinct();

            foreach (var professorId in professorConflitanteId)
            {
                var outroProfessor = await _professorRepository.GetByIdAsync(professorId, cancellationToken);
                if (outroProfessor is not null && outroProfessor.IsAtivo)
                {
                    var turma = turmasEncontradas[atribuicao.TurmaId];
                    var disciplina = disciplinasEncontradas[atribuicao.DisciplinaId];
                    return (ProfessorCreateResult.Fail(
                        $"A disciplina {disciplina.Nome} da turma {turma.Nome} já possui o professor {outroProfessor.NomeCompleto} ativo vinculado."),
                        nomeCompleto, email, usuarioCpf, atribuicoesVazias);
                }
            }
        }

        return (ProfessorCreateResult.Success(), nomeCompleto, email, usuarioCpf, atribuicoesRecebidas);
    }

    private async Task<(IReadOnlyList<Domain.Entities.Turma> Turmas, IReadOnlyList<Domain.Entities.Disciplina> Disciplinas, IReadOnlyList<Domain.Entities.Serie> Series)> CarregarTurmasDisciplinasESeriesAsync(CancellationToken cancellationToken)
    {
        var turmas = await _turmaRepository.GetAllAsync(null, cancellationToken);
        var disciplinas = await _disciplinaRepository.GetAllAsync(null, cancellationToken);
        var series = await _serieRepository.GetAllAsync(null, cancellationToken);
        return (turmas, disciplinas, series);
    }

    private static ProfessorAtribuicaoDto MontarAtribuicaoDto(
        Guid turmaId,
        Guid disciplinaId,
        IReadOnlyList<Domain.Entities.Turma> turmas,
        IReadOnlyList<Domain.Entities.Disciplina> disciplinas,
        IReadOnlyList<Domain.Entities.Serie> series)
    {
        var turma = turmas.FirstOrDefault(x => x.Id == turmaId);
        var disciplina = disciplinas.FirstOrDefault(x => x.Id == disciplinaId);
        var serie = turma is null ? null : series.FirstOrDefault(x => x.Id == turma.SerieId);

        return new ProfessorAtribuicaoDto(
            turmaId,
            turma?.Nome ?? "Turma não encontrada",
            serie?.Nome ?? "-",
            disciplinaId,
            disciplina?.Nome ?? "Disciplina não encontrada");
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
