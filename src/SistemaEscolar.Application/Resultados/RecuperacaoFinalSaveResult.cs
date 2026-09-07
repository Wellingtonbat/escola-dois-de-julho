namespace SistemaEscolar.Application.Resultados;

public sealed record RecuperacaoFinalSaveResult(bool Succeeded, string? ErrorMessage)
{
    public static RecuperacaoFinalSaveResult Success() => new(true, null);
    public static RecuperacaoFinalSaveResult Fail(string errorMessage) => new(false, errorMessage);
}
