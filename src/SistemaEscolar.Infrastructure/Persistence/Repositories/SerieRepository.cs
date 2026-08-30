using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class SerieRepository : ISerieRepository
{
    private readonly ApplicationDbContext _context;

    public SerieRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Serie>> GetAllAsync(SerieListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Series
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Busca))
        {
            var busca = filter.Busca.Trim();
            query = query.Where(x => x.Nome.Contains(busca));
        }

        if (filter?.IsAtiva.HasValue == true)
        {
            query = query.Where(x => x.IsAtiva == filter.IsAtiva.Value);
        }

        return await query
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Serie?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Series.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public Task<bool> NomeExisteAsync(string nome, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        return _context.Series
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Nome == nome && (!ignoreId.HasValue || x.Id != ignoreId.Value), cancellationToken);
    }

    public async Task AddAsync(Serie serie, CancellationToken cancellationToken = default)
    {
        _context.Series.Add(serie);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Serie serie, CancellationToken cancellationToken = default)
    {
        _context.Series.Update(serie);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Serie serie, CancellationToken cancellationToken = default)
    {
        serie.IsDeleted = true;
        _context.Series.Update(serie);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
