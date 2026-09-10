using SistemaEscolar.Application.Notas;

namespace SistemaEscolar.Tests.Support;

internal sealed class NotaServiceStub : INotaService
{
    private readonly IReadOnlyList<NotaListItemDto> _notas;

    public NotaServiceStub(IReadOnlyList<NotaListItemDto>? notas = null)
    {
        _notas = notas ?? Array.Empty<NotaListItemDto>();
    }

    public Task<IReadOnlyList<NotaListItemDto>> ListarAsync(NotaListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_notas);

    public Task<NotaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<NotaListItemDto?>(null);

    public Task<NotaCreateResult> CriarAsync(NotaCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(NotaCreateResult.Fail("not implemented in test stub"));

    public Task<NotaCreateResult> AtualizarAsync(Guid id, NotaCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(NotaCreateResult.Fail("not implemented in test stub"));

    public Task<bool> AlternarFinalizacaoAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
