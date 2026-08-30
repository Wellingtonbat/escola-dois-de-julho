using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class DisciplinaSerie : BaseEntity
{
    public Guid Id { get; set; }
    public Guid DisciplinaId { get; set; }
    public Guid SerieId { get; set; }
}
