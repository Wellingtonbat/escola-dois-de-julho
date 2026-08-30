using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class ProfessorRepository : IProfessorRepository
{
    private readonly ApplicationDbContext _context;

    public ProfessorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Professor>> GetAllAsync(ProfessorListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Professores
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Busca))
        {
            var busca = filter.Busca.Trim();
            query = query.Where(x =>
                x.NomeCompleto.Contains(busca) ||
                x.Email.Contains(busca));
        }

        if (filter?.IsAtivo.HasValue == true)
        {
            query = query.Where(x => x.IsAtivo == filter.IsAtivo.Value);
        }

        return await query
            .OrderBy(x => x.NomeCompleto)
            .ToListAsync(cancellationToken);
    }

    public async Task<Professor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Professores
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Professor?> GetByUsuarioCpfAsync(string usuarioCpf, CancellationToken cancellationToken = default)
    {
        return await _context.Professores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.UsuarioCpf == usuarioCpf, cancellationToken);
    }

    public async Task<bool> NomeEmailExisteAsync(string nomeCompleto, string email, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Professores
            .AsNoTracking()
            .AnyAsync(x =>
                !x.IsDeleted
                && x.NomeCompleto == nomeCompleto
                && x.Email == email
                && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
    }

    public async Task<bool> UsuarioCpfExisteAsync(string usuarioCpf, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Professores
            .AsNoTracking()
            .AnyAsync(x =>
                !x.IsDeleted
                && x.UsuarioCpf == usuarioCpf
                && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
    }

    public async Task AddAsync(Professor professor, CancellationToken cancellationToken = default)
    {
        _context.Professores.Add(professor);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Professor professor, CancellationToken cancellationToken = default)
    {
        _context.Professores.Update(professor);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Professor professor, CancellationToken cancellationToken = default)
    {
        professor.IsDeleted = true;
        _context.Professores.Update(professor);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
