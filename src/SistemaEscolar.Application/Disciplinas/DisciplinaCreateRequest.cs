namespace SistemaEscolar.Application.Disciplinas;

public sealed record DisciplinaCreateRequest(
    string Nome,
    string Codigo,
    IReadOnlyList<Guid> SerieIds,
    int CargaHoraria,
    bool IsAtiva);
