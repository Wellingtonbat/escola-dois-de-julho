namespace SistemaEscolar.Application.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsInRole(string role);
}
