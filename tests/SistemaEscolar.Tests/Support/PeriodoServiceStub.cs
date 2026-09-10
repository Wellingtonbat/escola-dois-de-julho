using SistemaEscolar.Application.Periodos;

namespace SistemaEscolar.Tests.Support;

internal sealed class PeriodoServiceStub : IPeriodoService
{
    private readonly IReadOnlyList<PeriodoListItemDto> _periodos;

    public PeriodoServiceStub(IReadOnlyList<PeriodoListItemDto>? periodos = null)
    {
        _periodos = periodos ?? Array.Empty<PeriodoListItemDto>();
    }

    public Task<IReadOnlyList<PeriodoListItemDto>> ListarAsync(PeriodoListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_periodos);

    public Task<PeriodoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<PeriodoListItemDto?>(null);

    public Task<PeriodoListItemDto?> ObterPorAnoETrimestreAsync(int anoLetivo, int trimestre, CancellationToken cancellationToken = default) =>
        Task.FromResult<PeriodoListItemDto?>(null);

    public Task<PeriodoCreateResult> CriarAsync(PeriodoCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(PeriodoCreateResult.Fail("not implemented in test stub"));

    public Task<PeriodoCreateResult> AtualizarAsync(Guid id, PeriodoCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(PeriodoCreateResult.Fail("not implemented in test stub"));

    public Task<PeriodoCreateResult> AbrirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(PeriodoCreateResult.Fail("not implemented in test stub"));

    public Task<PeriodoCreateResult> FecharAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(PeriodoCreateResult.Fail("not implemented in test stub"));

    public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
