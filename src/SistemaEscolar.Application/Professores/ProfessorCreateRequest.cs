namespace SistemaEscolar.Application.Professores;

public sealed record ProfessorCreateRequest(
    string NomeCompleto,
    string Email,
    string UsuarioCpf,
    IReadOnlyList<ProfessorAtribuicaoInput> Atribuicoes,
    bool IsAtivo,
    string? Senha = null);
