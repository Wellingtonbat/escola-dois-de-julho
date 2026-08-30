namespace SistemaEscolar.Application.Abstractions;

public interface IAccountProvisioner
{
    Task<AccountProvisionResult> CriarOuReiniciarContaAsync(
        string cpf,
        string nomeCompleto,
        string email,
        string senha,
        string role,
        CancellationToken cancellationToken = default);
}

public sealed record AccountProvisionResult(bool Succeeded, string? ErrorMessage)
{
    public static AccountProvisionResult Success() => new(true, null);

    public static AccountProvisionResult Fail(string errorMessage) => new(false, errorMessage);
}
