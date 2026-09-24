namespace SistemaEscolar.Application.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }

    // Nome completo do usuário logado (vem de um claim gravado no login). Pode ser nulo em sessões
    // iniciadas antes desse claim existir; a auditoria então resolve o nome pela conta.
    string? FullName { get; }

    bool IsInRole(string role);
}
