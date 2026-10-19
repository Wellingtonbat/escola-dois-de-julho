namespace SistemaEscolar.Application.Usuarios;

public sealed record UsuarioCreateResult(bool Succeeded, string? ErrorMessage, string? SuccessMessage = null)
{
    public static UsuarioCreateResult Success(string? mensagem = null) => new(true, null, mensagem);

    public static UsuarioCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
