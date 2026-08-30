namespace SistemaEscolar.Application.Resultados;

public sealed record ResultadoAcademicoDto(
    Guid AlunoId,
    string AlunoNome,
    string Disciplina,
    string Turma,
    string Serie,
    int AnoLetivo,
    decimal MediaFinal,
    string Situacao,
    string Motivo);