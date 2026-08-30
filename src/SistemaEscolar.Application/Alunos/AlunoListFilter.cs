namespace SistemaEscolar.Application.Alunos;

public sealed record AlunoListFilter(string? Busca, string? Serie, string? Turma, bool? IsAtivo);
