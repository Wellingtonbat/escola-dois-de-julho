namespace SistemaEscolar.Application.Series;

public sealed record SerieCreateResult(bool Succeeded, string? ErrorMessage = null)
{
    public static SerieCreateResult Success() => new(true);
    public static SerieCreateResult Fail(string errorMessage) => new(false, errorMessage);
}
