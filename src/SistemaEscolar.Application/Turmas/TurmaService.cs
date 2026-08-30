using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Application.Turmas;

public sealed class TurmaService : ITurmaService
{
    private readonly ITurmaRepository _turmaRepository;
    private readonly ISerieRepository _serieRepository;

    public TurmaService(ITurmaRepository turmaRepository, ISerieRepository serieRepository)
    {
        _turmaRepository = turmaRepository;
        _serieRepository = serieRepository;
    }

    public async Task<IReadOnlyList<TurmaListItemDto>> ListarAsync(TurmaListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var turmas = await _turmaRepository.GetAllAsync(filter, cancellationToken);
        var series = await _serieRepository.GetAllAsync(null, cancellationToken);

        return turmas
            .Select(x => MontarDto(x, series))
            .OrderBy(x => x.AnoLetivo)
            .ThenBy(x => x.SerieNome)
            .ThenBy(x => x.Nome)
            .ToList();
    }

    public async Task<TurmaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var turma = await _turmaRepository.GetByIdAsync(id, cancellationToken);
        if (turma is null)
        {
            return null;
        }

        var series = await _serieRepository.GetAllAsync(null, cancellationToken);
        return MontarDto(turma, series);
    }

    public async Task<TurmaCreateResult> CriarAsync(TurmaCreateRequest request, CancellationToken cancellationToken = default)
    {
        var nome = (request.Nome ?? string.Empty).Trim();
        var turno = (request.Turno ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return TurmaCreateResult.Fail("O nome da turma é obrigatório.");
        }

        if (request.SerieId == Guid.Empty)
        {
            return TurmaCreateResult.Fail("A série da turma é obrigatória.");
        }

        if (await _serieRepository.GetByIdAsync(request.SerieId, cancellationToken) is null)
        {
            return TurmaCreateResult.Fail("A série selecionada não foi encontrada.");
        }

        if (request.AnoLetivo < 2000 || request.AnoLetivo > 2100)
        {
            return TurmaCreateResult.Fail("Informe um ano letivo válido.");
        }

        if (await _turmaRepository.NomeAnoExisteAsync(nome, request.AnoLetivo, null, cancellationToken))
        {
            return TurmaCreateResult.Fail("Já existe turma cadastrada com este nome e ano letivo.");
        }

        var turma = new Domain.Entities.Turma
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            SerieId = request.SerieId,
            Turno = string.IsNullOrWhiteSpace(turno) ? "Não informado" : turno,
            AnoLetivo = request.AnoLetivo,
            IsAtiva = request.IsAtiva
        };

        await _turmaRepository.AddAsync(turma, cancellationToken);
        return TurmaCreateResult.Success();
    }

    public async Task<TurmaCreateResult> AtualizarAsync(Guid id, TurmaCreateRequest request, CancellationToken cancellationToken = default)
    {
        var turma = await _turmaRepository.GetByIdAsync(id, cancellationToken);
        if (turma is null)
        {
            return TurmaCreateResult.Fail("Turma não encontrada.");
        }

        var nome = (request.Nome ?? string.Empty).Trim();
        var turno = (request.Turno ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return TurmaCreateResult.Fail("O nome da turma é obrigatório.");
        }

        if (request.SerieId == Guid.Empty)
        {
            return TurmaCreateResult.Fail("A série da turma é obrigatória.");
        }

        if (await _serieRepository.GetByIdAsync(request.SerieId, cancellationToken) is null)
        {
            return TurmaCreateResult.Fail("A série selecionada não foi encontrada.");
        }

        if (request.AnoLetivo < 2000 || request.AnoLetivo > 2100)
        {
            return TurmaCreateResult.Fail("Informe um ano letivo válido.");
        }

        if (await _turmaRepository.NomeAnoExisteAsync(nome, request.AnoLetivo, id, cancellationToken))
        {
            return TurmaCreateResult.Fail("Já existe turma cadastrada com este nome e ano letivo.");
        }

        turma.Nome = nome;
        turma.SerieId = request.SerieId;
        turma.Turno = string.IsNullOrWhiteSpace(turno) ? "Não informado" : turno;
        turma.AnoLetivo = request.AnoLetivo;
        turma.IsAtiva = request.IsAtiva;

        await _turmaRepository.UpdateAsync(turma, cancellationToken);
        return TurmaCreateResult.Success();
    }

    public async Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var turma = await _turmaRepository.GetByIdAsync(id, cancellationToken);
        if (turma is null)
        {
            return false;
        }

        turma.IsAtiva = !turma.IsAtiva;
        await _turmaRepository.UpdateAsync(turma, cancellationToken);
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var turma = await _turmaRepository.GetByIdAsync(id, cancellationToken);
        if (turma is null)
        {
            return false;
        }

        await _turmaRepository.SoftDeleteAsync(turma, cancellationToken);
        return true;
    }

    private static TurmaListItemDto MontarDto(Domain.Entities.Turma turma, IReadOnlyList<Domain.Entities.Serie> series)
    {
        var serie = series.FirstOrDefault(x => x.Id == turma.SerieId);
        return new TurmaListItemDto(turma.Id, turma.Nome, turma.SerieId, serie?.Nome ?? "Série não encontrada", turma.Turno, turma.AnoLetivo, turma.IsAtiva);
    }
}
