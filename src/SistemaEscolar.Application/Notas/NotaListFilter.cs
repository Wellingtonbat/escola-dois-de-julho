namespace SistemaEscolar.Application.Notas;

public sealed record NotaListFilter(
    string? Busca,
    int? AnoLetivo,
    int? Trimestre,
    bool? IsFinalizada,
    Guid? TurmaId = null,
    Guid? DisciplinaId = null);
