namespace SistemaEscolar.Application.Periodos;

public sealed record PeriodoCreateResult(bool Succeeded, string? ErrorMessage = null)
{
    public static PeriodoCreateResult Success() => new(true);
    public static PeriodoCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
