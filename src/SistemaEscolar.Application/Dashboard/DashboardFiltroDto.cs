namespace SistemaEscolar.Application.Dashboard;

public sealed record DashboardFiltroDto(
    int AnoLetivo,
    int? Trimestre,
    Guid? ProfessorId,
    Guid? TurmaId,
    Guid? DisciplinaId);
