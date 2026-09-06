namespace SistemaEscolar.Application.Alunos;

public sealed record AlunoListFilter(string? Busca, string? Serie, Guid? TurmaId, bool? IsAtivo);
