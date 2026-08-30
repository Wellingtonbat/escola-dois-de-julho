using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class DisciplinaSerieRepository : IDisciplinaSerieRepository
{
    private readonly ApplicationDbContext _context;

    public DisciplinaSerieRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<(Guid DisciplinaId, Guid SerieId)>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaSeries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new ValueTuple<Guid, Guid>(x.DisciplinaId, x.SerieId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSerieIdsByDisciplinaIdAsync(Guid disciplinaId, CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaSeries
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.DisciplinaId == disciplinaId)
            .Select(x => x.SerieId)
            .ToListAsync(cancellationToken);
    }

    public async Task SubstituirAsync(Guid disciplinaId, IReadOnlyList<Guid> serieIds, CancellationToken cancellationToken = default)
    {
        var existentes = await _context.DisciplinaSeries
            .Where(x => !x.IsDeleted && x.DisciplinaId == disciplinaId)
            .ToListAsync(cancellationToken);

        foreach (var existente in existentes)
        {
            existente.IsDeleted = true;
        }

        foreach (var serieId in serieIds)
        {
            _context.DisciplinaSeries.Add(new DisciplinaSerie
            {
                Id = Guid.NewGuid(),
                DisciplinaId = disciplinaId,
                SerieId = serieId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
