namespace SistemaEscolar.Application.Usuarios;

public sealed record UsuarioCreateResult(bool Succeeded, string? ErrorMessage)
{
    public static UsuarioCreateResult Success() => new(true, null);

    public static UsuarioCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
