namespace SistemaEscolar.Application.Alunos;

public sealed record AlunoCreateRequest(
    string Cpf,
    string NomeCompleto,
    DateTime DataNascimento,
    int AnoLetivo,
    Guid SerieId,
    Guid? TurmaId,
    bool IsAtivo);
