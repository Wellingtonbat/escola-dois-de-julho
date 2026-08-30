using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class PeriodoRepository : IPeriodoRepository
{
    private readonly ApplicationDbContext _context;

    public PeriodoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PeriodoLancamento>> GetAllAsync(PeriodoListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PeriodosLancamento
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (filter?.AnoLetivo.HasValue == true)
        {
            query = query.Where(x => x.AnoLetivo == filter.AnoLetivo.Value);
        }

        if (filter?.Trimestre.HasValue == true)
        {
            query = query.Where(x => x.Bimestre == filter.Trimestre.Value);
        }

        if (filter?.IsAberto.HasValue == true)
        {
            query = query.Where(x => x.IsAberto == filter.IsAberto.Value);
        }

        return await query
            .OrderByDescending(x => x.AnoLetivo)
            .ThenBy(x => x.Bimestre)
            .ToListAsync(cancellationToken);
    }

    public Task<PeriodoLancamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.PeriodosLancamento
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public Task<PeriodoLancamento?> GetByAnoETrimestreAsync(int anoLetivo, int trimestre, CancellationToken cancellationToken = default)
    {
        return _context.PeriodosLancamento
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => !x.IsDeleted && x.AnoLetivo == anoLetivo && x.Bimestre == trimestre,
                cancellationToken);
    }

    public Task<bool> ExisteAnoETrimestreAsync(int anoLetivo, int trimestre, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        return _context.PeriodosLancamento
            .AsNoTracking()
            .AnyAsync(
                x => !x.IsDeleted
                     && x.AnoLetivo == anoLetivo
                     && x.Bimestre == trimestre
                     && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
    }

    public Task<bool> ExisteSobreposicaoAsync(DateTime dataInicial, DateTime dataFinal, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        var start = dataInicial.Date;
        var end = dataFinal.Date;

        return _context.PeriodosLancamento
            .AsNoTracking()
            .AnyAsync(
                x => !x.IsDeleted
                     && (!ignoreId.HasValue || x.Id != ignoreId.Value)
                     && x.DataInicial <= end
                     && start <= x.DataFinal,
                cancellationToken);
    }

    public async Task AddAsync(PeriodoLancamento periodo, CancellationToken cancellationToken = default)
    {
        _context.PeriodosLancamento.Add(periodo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PeriodoLancamento periodo, CancellationToken cancellationToken = default)
    {
        _context.PeriodosLancamento.Update(periodo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(PeriodoLancamento periodo, CancellationToken cancellationToken = default)
    {
        periodo.IsDeleted = true;
        _context.PeriodosLancamento.Update(periodo);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
