using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class Serie : BaseEntity
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool IsAtiva { get; set; } = true;
}
