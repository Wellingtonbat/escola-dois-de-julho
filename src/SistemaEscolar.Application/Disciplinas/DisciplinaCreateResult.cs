namespace SistemaEscolar.Application.Disciplinas;

public sealed record DisciplinaCreateResult(bool Succeeded, string? ErrorMessage = null)
{
    public static DisciplinaCreateResult Success() => new(true);
    public static DisciplinaCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
