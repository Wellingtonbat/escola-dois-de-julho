using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Series;

public interface ISerieRepository
{
    Task<IReadOnlyList<Serie>> GetAllAsync(SerieListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Serie?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NomeExisteAsync(string nome, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Serie serie, CancellationToken cancellationToken = default);
    Task UpdateAsync(Serie serie, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Serie serie, CancellationToken cancellationToken = default);
}
