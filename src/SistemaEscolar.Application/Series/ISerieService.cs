namespace SistemaEscolar.Application.Series;

public interface ISerieService
{
    Task<IReadOnlyList<SerieListItemDto>> ListarAsync(SerieListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<SerieListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SerieCreateResult> CriarAsync(SerieCreateRequest request, CancellationToken cancellationToken = default);
    Task<SerieCreateResult> AtualizarAsync(Guid id, SerieCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
}
