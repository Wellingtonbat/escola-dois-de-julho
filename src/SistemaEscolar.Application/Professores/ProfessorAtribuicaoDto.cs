namespace SistemaEscolar.Application.Professores;

public sealed record ProfessorAtribuicaoDto(
    Guid TurmaId,
    string TurmaNome,
    string SerieNome,
    Guid DisciplinaId,
    string DisciplinaNome);
