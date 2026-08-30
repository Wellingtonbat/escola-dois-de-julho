namespace SistemaEscolar.Application.Series;

public sealed class SerieService : ISerieService
{
    private readonly ISerieRepository _serieRepository;

    public SerieService(ISerieRepository serieRepository)
    {
        _serieRepository = serieRepository;
    }

    public async Task<IReadOnlyList<SerieListItemDto>> ListarAsync(SerieListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var series = await _serieRepository.GetAllAsync(filter, cancellationToken);

        return series
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .Select(x => new SerieListItemDto(x.Id, x.Nome, x.Ordem, x.IsAtiva))
            .ToList();
    }

    public async Task<SerieListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serie = await _serieRepository.GetByIdAsync(id, cancellationToken);
        if (serie is null)
        {
            return null;
        }

        return new SerieListItemDto(serie.Id, serie.Nome, serie.Ordem, serie.IsAtiva);
    }

    public async Task<SerieCreateResult> CriarAsync(SerieCreateRequest request, CancellationToken cancellationToken = default)
    {
        var nome = (request.Nome ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return SerieCreateResult.Fail("O nome da série é obrigatório.");
        }

        if (request.Ordem <= 0)
        {
            return SerieCreateResult.Fail("A ordem da série deve ser maior que zero.");
        }

        if (await _serieRepository.NomeExisteAsync(nome, null, cancellationToken))
        {
            return SerieCreateResult.Fail("Já existe série cadastrada com este nome.");
        }

        var serie = new Domain.Entities.Serie
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Ordem = request.Ordem,
            IsAtiva = request.IsAtiva
        };

        await _serieRepository.AddAsync(serie, cancellationToken);
        return SerieCreateResult.Success();
    }

    public async Task<SerieCreateResult> AtualizarAsync(Guid id, SerieCreateRequest request, CancellationToken cancellationToken = default)
    {
        var serie = await _serieRepository.GetByIdAsync(id, cancellationToken);
        if (serie is null)
        {
            return SerieCreateResult.Fail("Série não encontrada.");
        }

        var nome = (request.Nome ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return SerieCreateResult.Fail("O nome da série é obrigatório.");
        }

        if (request.Ordem <= 0)
        {
            return SerieCreateResult.Fail("A ordem da série deve ser maior que zero.");
        }

        if (await _serieRepository.NomeExisteAsync(nome, id, cancellationToken))
        {
            return SerieCreateResult.Fail("Já existe série cadastrada com este nome.");
        }

        serie.Nome = nome;
        serie.Ordem = request.Ordem;
        serie.IsAtiva = request.IsAtiva;

        await _serieRepository.UpdateAsync(serie, cancellationToken);
        return SerieCreateResult.Success();
    }

    public async Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serie = await _serieRepository.GetByIdAsync(id, cancellationToken);
        if (serie is null)
        {
            return false;
        }

        serie.IsAtiva = !serie.IsAtiva;
        await _serieRepository.UpdateAsync(serie, cancellationToken);
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serie = await _serieRepository.GetByIdAsync(id, cancellationToken);
        if (serie is null)
        {
            return false;
        }

        await _serieRepository.SoftDeleteAsync(serie, cancellationToken);
        return true;
    }
}
