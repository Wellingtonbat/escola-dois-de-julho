namespace SistemaEscolar.Application.Series;

public sealed record SerieCreateRequest(
    string Nome,
    int Ordem,
    bool IsAtiva);
