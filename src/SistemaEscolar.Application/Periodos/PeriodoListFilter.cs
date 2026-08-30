namespace SistemaEscolar.Application.Periodos;

public sealed record PeriodoListFilter(int? AnoLetivo, int? Trimestre, bool? IsAberto);
