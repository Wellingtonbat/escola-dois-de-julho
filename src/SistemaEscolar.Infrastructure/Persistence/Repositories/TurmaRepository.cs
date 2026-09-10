using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Turmas;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class TurmaRepository : ITurmaRepository
{
    private readonly ApplicationDbContext _context;

    public TurmaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Turma>> GetAllAsync(TurmaListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Turmas
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Busca))
        {
            var busca = filter.Busca.Trim().ToLower();
            query = query.Where(x =>
                x.Nome.ToLower().Contains(busca) ||
                x.Turno.ToLower().Contains(busca));
        }

        if (filter?.SerieId.HasValue == true)
        {
            query = query.Where(x => x.SerieId == filter.SerieId.Value);
        }

        if (filter?.AnoLetivo.HasValue == true)
        {
            query = query.Where(x => x.AnoLetivo == filter.AnoLetivo.Value);
        }

        if (filter?.IsAtiva.HasValue == true)
        {
            query = query.Where(x => x.IsAtiva == filter.IsAtiva.Value);
        }

        return await query
            .OrderBy(x => x.AnoLetivo)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<Turma?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Turmas
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<bool> NomeAnoExisteAsync(string nome, int anoLetivo, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Turmas
            .AsNoTracking()
            .AnyAsync(x =>
                !x.IsDeleted
                && x.Nome == nome
                && x.AnoLetivo == anoLetivo
                && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
    }

    public async Task AddAsync(Turma turma, CancellationToken cancellationToken = default)
    {
        _context.Turmas.Add(turma);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Turma turma, CancellationToken cancellationToken = default)
    {
        _context.Turmas.Update(turma);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Turma turma, CancellationToken cancellationToken = default)
    {
        turma.IsDeleted = true;
        _context.Turmas.Update(turma);
        await _context.SaveChangesAsync(cancellationToken);
    }
}