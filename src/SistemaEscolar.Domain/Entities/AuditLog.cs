using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public long Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? KeyValues { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    // Quem fez a alteração (o Id do usuário fica em CreatedBy). Guardamos também uma cópia do login (CPF)
    // e do nome no momento da ação, para que o histórico continue legível mesmo se a conta for excluída.
    // Nulos = ação feita sem usuário logado (carga inicial do sistema) ou registro anterior a esta coluna.
    public string? UserName { get; set; }
    public string? UserFullName { get; set; }
}
