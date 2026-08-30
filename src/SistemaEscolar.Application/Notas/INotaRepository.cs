using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Notas;

public interface INotaRepository
{
    Task<IReadOnlyList<NotaListItemDto>> GetAllAsync(NotaListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Nota?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> LancamentoDuplicadoExisteAsync(
        Guid alunoId,
        Guid disciplinaId,
        Guid periodoLancamentoId,
        Guid? ignoreId = null,
        CancellationToken cancellationToken = default);
    Task<bool> TemPendenciasAsync(Guid periodoLancamentoId, CancellationToken cancellationToken = default);
    Task AddAsync(Nota nota, CancellationToken cancellationToken = default);
    Task UpdateAsync(Nota nota, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Nota nota, CancellationToken cancellationToken = default);
}
