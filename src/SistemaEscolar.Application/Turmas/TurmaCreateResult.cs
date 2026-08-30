namespace SistemaEscolar.Application.Turmas;

public sealed record TurmaCreateResult(bool Succeeded, string? ErrorMessage = null)
{
    public static TurmaCreateResult Success() => new(true);
    public static TurmaCreateResult Fail(string errorMessage) => new(false, errorMessage);
}