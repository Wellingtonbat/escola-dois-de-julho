using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class Turma : BaseEntity
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid SerieId { get; set; }
    public string Turno { get; set; } = string.Empty;
    public int AnoLetivo { get; set; }
    public bool IsAtiva { get; set; } = true;
}