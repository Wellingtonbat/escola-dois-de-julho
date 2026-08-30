using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class Disciplina : BaseEntity
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public int CargaHoraria { get; set; }
    public bool IsAtiva { get; set; } = true;
}
