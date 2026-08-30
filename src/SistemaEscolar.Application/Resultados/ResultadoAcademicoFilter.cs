namespace SistemaEscolar.Application.Resultados;

public sealed record ResultadoAcademicoFilter(
    int AnoLetivo,
    string? Turma,
    string? Serie,
    string Situacao);