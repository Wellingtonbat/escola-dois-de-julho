namespace SistemaEscolar.Application.Alunos;

public sealed record AlunoCreateResult(bool Succeeded, string? ErrorMessage)
{
    public static AlunoCreateResult Success() => new(true, null);

    public static AlunoCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
