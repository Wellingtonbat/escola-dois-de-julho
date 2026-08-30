using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class DisciplinaRepository : IDisciplinaRepository
{
    private readonly ApplicationDbContext _context;

    public DisciplinaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Disciplina>> GetAllAsync(DisciplinaListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Disciplinas
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Busca))
        {
            var busca = filter.Busca.Trim();
            query = query.Where(x => x.Nome.Contains(busca) || x.Codigo.Contains(busca));
        }

        if (filter?.IsAtiva.HasValue == true)
        {
            query = query.Where(x => x.IsAtiva == filter.IsAtiva.Value);
        }

        return await query
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<Disciplina?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Disciplinas
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<bool> CodigoExisteAsync(string codigo, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Disciplinas
            .AsNoTracking()
            .AnyAsync(x =>
                !x.IsDeleted
                && x.Codigo == codigo
                && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
    }

    public async Task AddAsync(Disciplina disciplina, CancellationToken cancellationToken = default)
    {
        _context.Disciplinas.Add(disciplina);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Disciplina disciplina, CancellationToken cancellationToken = default)
    {
        _context.Disciplinas.Update(disciplina);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Disciplina disciplina, CancellationToken cancellationToken = default)
    {
        disciplina.IsDeleted = true;
        _context.Disciplinas.Update(disciplina);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
