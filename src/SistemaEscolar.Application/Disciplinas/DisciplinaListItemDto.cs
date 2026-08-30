namespace SistemaEscolar.Application.Disciplinas;

public sealed record DisciplinaListItemDto(
    Guid Id,
    string Nome,
    string Codigo,
    IReadOnlyList<DisciplinaSerieDto> Series,
    int CargaHoraria,
    bool IsAtiva)
{
    public string SeriesResumo =>
        string.Join(", ", Series.Select(x => x.SerieNome).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
}
