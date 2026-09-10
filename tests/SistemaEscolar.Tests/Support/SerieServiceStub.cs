using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Tests.Support;

internal sealed class SerieServiceStub : ISerieService
{
    private readonly IReadOnlyList<SerieListItemDto> _series;

    public SerieServiceStub(IReadOnlyList<SerieListItemDto>? series = null)
    {
        _series = series ?? Array.Empty<SerieListItemDto>();
    }

    public Task<IReadOnlyList<SerieListItemDto>> ListarAsync(SerieListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_series);

    public Task<SerieListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<SerieListItemDto?>(null);

    public Task<SerieCreateResult> CriarAsync(SerieCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(SerieCreateResult.Fail("not implemented in test stub"));

    public Task<SerieCreateResult> AtualizarAsync(Guid id, SerieCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(SerieCreateResult.Fail("not implemented in test stub"));

    public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
