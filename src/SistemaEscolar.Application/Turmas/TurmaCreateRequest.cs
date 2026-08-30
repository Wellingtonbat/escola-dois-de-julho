namespace SistemaEscolar.Application.Turmas;

public sealed record TurmaCreateRequest(
    string Nome,
    Guid SerieId,
    string Turno,
    int AnoLetivo,
    bool IsAtiva);
