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

    public bool IsInRole(string role)
    {
        return _roles.Contains(role);
    }
}
