namespace SistemaEscolar.Application.Turmas;

public sealed record TurmaListFilter(string? Busca, Guid? SerieId, int? AnoLetivo, bool? IsAtiva);
