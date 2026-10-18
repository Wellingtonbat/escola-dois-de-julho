using SistemaEscolar.Application.Abstractions;

namespace SistemaEscolar.Tests.Support;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    private readonly HashSet<string> _roles;

    public FakeCurrentUserService(string? userName, params string[] roles)
    {
        UserName = userName;
        _roles = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
    }

    public string? UserId => null;

    public string? UserName { get; }

    public string? FullName => null;

    public bool IsInRole(string role)
    {
        return _roles.Contains(role);
    }

    // Permissões padrão de cada perfil, como gravadas na primeira inicialização.
    public bool TemPermissao(string permissao) =>
        _roles.Any(perfil => Permissoes.PadraoDoPerfil(perfil).Contains(permissao));
}
