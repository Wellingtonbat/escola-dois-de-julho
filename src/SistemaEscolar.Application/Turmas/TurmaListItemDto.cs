namespace SistemaEscolar.Application.Turmas;

public sealed record TurmaListItemDto(
    Guid Id,
    string Nome,
    Guid SerieId,
    string SerieNome,
    string Turno,
    int AnoLetivo,
    bool IsAtiva);
