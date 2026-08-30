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
}
