namespace SistemaEscolar.Application.Usuarios;

public sealed record UsuarioCreateRequest(
    string NomeCompleto,
    string Email,
    string Cpf,
    string Perfil,
    bool IsAtivo,
    string? Senha = null);
