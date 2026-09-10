namespace SistemaEscolar.Application.Resultados;

public sealed record ResultadoAcademicoDto(
    Guid AlunoId,
    string AlunoNome,
    Guid DisciplinaId,
    string Disciplina,
    string Turma,
    string Serie,
    string ProfessorNome,
    int AnoLetivo,
    decimal MediaFinal,
    decimal? RecuperacaoFinal,
    bool RecuperacaoFinalDisponivel,
    decimal ResultadoFinalAno,
    string Situacao,
    string Motivo);
