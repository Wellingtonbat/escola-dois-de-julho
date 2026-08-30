namespace SistemaEscolar.Application.Professores;

public sealed record ProfessorEscopoDto(
    Guid ProfessorId,
    IReadOnlyList<Guid> TurmaIds,
    IReadOnlyList<string> TurmaNomes,
    IReadOnlyList<Guid> DisciplinaIds,
    IReadOnlyList<Guid> SerieIds);
