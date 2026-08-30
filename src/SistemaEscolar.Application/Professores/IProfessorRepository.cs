using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Professores;

public interface IProfessorRepository
{
    Task<IReadOnlyList<Professor>> GetAllAsync(ProfessorListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Professor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Professor?> GetByUsuarioCpfAsync(string usuarioCpf, CancellationToken cancellationToken = default);
    Task<bool> NomeEmailExisteAsync(string nomeCompleto, string email, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task<bool> UsuarioCpfExisteAsync(string usuarioCpf, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Professor professor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Professor professor, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Professor professor, CancellationToken cancellationToken = default);
}
