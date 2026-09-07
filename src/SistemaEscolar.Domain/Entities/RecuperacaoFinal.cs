using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class RecuperacaoFinal : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AlunoId { get; set; }
    public Guid DisciplinaId { get; set; }
    public int AnoLetivo { get; set; }
    public decimal Valor { get; set; }
}
