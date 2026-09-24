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

    // Concede ou revoga um perfil adicional de uma conta existente (ex.: Vice-Diretor de um professor),
    // sem alterar os demais perfis da conta.
    Task<AccountProvisionResult> DefinirPerfilAsync(
        string cpf,
        string role,
        bool possuiPerfil,
        CancellationToken cancellationToken = default);

    // CPFs (nome de usuário) das contas que possuem o perfil informado.
    Task<IReadOnlySet<string>> ListarCpfsPorPerfilAsync(
        string role,
        CancellationToken cancellationToken = default);
}

public sealed record AccountProvisionResult(bool Succeeded, string? ErrorMessage)
{
    public static AccountProvisionResult Success() => new(true, null);

    public static AccountProvisionResult Fail(string errorMessage) => new(false, errorMessage);
}
