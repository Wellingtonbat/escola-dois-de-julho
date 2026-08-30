namespace SistemaEscolar.Application.Usuarios;

public sealed record UsuarioListItemDto(
    Guid Id,
    string NomeCompleto,
    string Email,
    string Cpf,
    string Perfil,
    bool IsAtivo);
