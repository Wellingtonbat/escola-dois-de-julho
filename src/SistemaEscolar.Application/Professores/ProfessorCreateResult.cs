namespace SistemaEscolar.Application.Professores;

public sealed record ProfessorCreateResult(bool Succeeded, string? ErrorMessage = null)
{
    public static ProfessorCreateResult Success() => new(true);
    public static ProfessorCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
