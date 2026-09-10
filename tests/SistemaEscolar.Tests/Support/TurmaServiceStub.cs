using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Tests.Support;

internal sealed class TurmaServiceStub : ITurmaService
{
    private readonly IReadOnlyList<TurmaListItemDto> _turmas;

    public TurmaServiceStub(IReadOnlyList<TurmaListItemDto>? turmas = null)
    {
        _turmas = turmas ?? Array.Empty<TurmaListItemDto>();
    }

    public Task<IReadOnlyList<TurmaListItemDto>> ListarAsync(TurmaListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_turmas);

    public Task<TurmaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<TurmaListItemDto?>(null);

    public Task<TurmaCreateResult> CriarAsync(TurmaCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(TurmaCreateResult.Fail("not implemented in test stub"));

    public Task<TurmaCreateResult> AtualizarAsync(Guid id, TurmaCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(TurmaCreateResult.Fail("not implemented in test stub"));

    public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
