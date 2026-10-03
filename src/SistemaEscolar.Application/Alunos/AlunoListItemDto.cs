namespace SistemaEscolar.Application.Alunos;

public sealed record AlunoListItemDto(
    Guid Id,
    string Matricula,
    string? MatriculaPrefeitura,
    string Cpf,
    string NomeCompleto,
    DateTime DataNascimento,
    int AnoLetivo,
    Guid SerieId,
    string Serie,
    Guid? TurmaId,
    string? TurmaNome,
    bool IsAtivo);
