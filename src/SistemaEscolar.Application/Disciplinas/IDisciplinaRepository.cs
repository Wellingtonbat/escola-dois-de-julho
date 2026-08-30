using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Disciplinas;

public interface IDisciplinaRepository
{
    Task<IReadOnlyList<Disciplina>> GetAllAsync(DisciplinaListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Disciplina?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodigoExisteAsync(string codigo, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Disciplina disciplina, CancellationToken cancellationToken = default);
    Task UpdateAsync(Disciplina disciplina, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Disciplina disciplina, CancellationToken cancellationToken = default);
}
