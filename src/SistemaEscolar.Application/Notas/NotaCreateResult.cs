namespace SistemaEscolar.Application.Notas;

public sealed record NotaCreateResult(bool Succeeded, string? ErrorMessage = null)
{
    public static NotaCreateResult Success() => new(true);
    public static NotaCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
