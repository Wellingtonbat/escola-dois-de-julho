namespace SistemaEscolar.Application.Professores;

public sealed record ProfessorCreateRequest(
    string NomeCompleto,
    string Email,
    string UsuarioCpf,
    IReadOnlyList<ProfessorAtribuicaoInput> Atribuicoes,
    bool IsAtivo,
    string? Senha = null,
    // Perfil adicional de Vice-Diretor para o professor. null = não alterar (quem não pode definir
    // esse perfil não envia o campo, e a edição não deve revogá-lo sem querer).
    bool? IsViceDiretor = null);
