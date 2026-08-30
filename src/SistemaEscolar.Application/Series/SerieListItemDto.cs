namespace SistemaEscolar.Application.Series;

public sealed record SerieListItemDto(
    Guid Id,
    string Nome,
    int Ordem,
    bool IsAtiva);
