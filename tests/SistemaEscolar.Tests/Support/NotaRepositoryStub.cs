using SistemaEscolar.Application.Notas;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Tests.Support;

internal sealed class NotaRepositoryStub : INotaRepository
{
    private readonly IReadOnlyList<NotaListItemDto> _notas;

    public NotaRepositoryStub(IReadOnlyList<NotaListItemDto>? notas = null)
    {
        _notas = notas ?? Array.Empty<NotaListItemDto>();
    }

    public Task<IReadOnlyList<NotaListItemDto>> GetAllAsync(NotaListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_notas);

    public Task<Nota?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Nota?>(null);

    public Task<bool> LancamentoDuplicadoExisteAsync(
        Guid alunoId,
        Guid disciplinaId,
        Guid periodoLancamentoId,
        Guid? ignoreId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> TemPendenciasAsync(Guid periodoLancamentoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task AddAsync(Nota nota, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpdateAsync(Nota nota, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SoftDeleteAsync(Nota nota, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
