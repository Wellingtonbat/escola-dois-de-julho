using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class Aluno : BaseEntity
{
    public Guid Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public int AnoLetivo { get; set; }
    public Guid SerieId { get; set; }
    public Guid? TurmaId { get; set; }
    public bool IsAtivo { get; set; } = true;
}
