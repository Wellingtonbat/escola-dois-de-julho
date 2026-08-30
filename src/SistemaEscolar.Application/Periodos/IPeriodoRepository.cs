using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Periodos;

public interface IPeriodoRepository
{
    Task<IReadOnlyList<PeriodoLancamento>> GetAllAsync(PeriodoListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PeriodoLancamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PeriodoLancamento?> GetByAnoETrimestreAsync(int anoLetivo, int trimestre, CancellationToken cancellationToken = default);
    Task<bool> ExisteAnoETrimestreAsync(int anoLetivo, int trimestre, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task<bool> ExisteSobreposicaoAsync(DateTime dataInicial, DateTime dataFinal, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task AddAsync(PeriodoLancamento periodo, CancellationToken cancellationToken = default);
    Task UpdateAsync(PeriodoLancamento periodo, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(PeriodoLancamento periodo, CancellationToken cancellationToken = default);
}
