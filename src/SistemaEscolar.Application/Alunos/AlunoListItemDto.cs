namespace SistemaEscolar.Application.Alunos;

public sealed record AlunoListItemDto(
    Guid Id,
    string Matricula,
    string Cpf,
    string NomeCompleto,
    DateTime DataNascimento,
    int AnoLetivo,
    Guid SerieId,
    string Serie,
    string? Turma,
    bool IsAtivo);
