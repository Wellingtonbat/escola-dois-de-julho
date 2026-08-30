using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Turmas;

public interface ITurmaRepository
{
    Task<IReadOnlyList<Turma>> GetAllAsync(TurmaListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Turma?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NomeAnoExisteAsync(string nome, int anoLetivo, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Turma turma, CancellationToken cancellationToken = default);
    Task UpdateAsync(Turma turma, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Turma turma, CancellationToken cancellationToken = default);
}