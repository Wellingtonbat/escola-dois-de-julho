using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class ProfessorAtribuicao : BaseEntity
{
    public Guid Id { get; set; }
    public Guid ProfessorId { get; set; }
    public Guid TurmaId { get; set; }
    public Guid DisciplinaId { get; set; }
}
