namespace SistemaEscolar.Application.Disciplinas;

public interface IDisciplinaSerieRepository
{
    Task<IReadOnlyList<(Guid DisciplinaId, Guid SerieId)>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetSerieIdsByDisciplinaIdAsync(Guid disciplinaId, CancellationToken cancellationToken = default);
    Task SubstituirAsync(Guid disciplinaId, IReadOnlyList<Guid> serieIds, CancellationToken cancellationToken = default);
}
