using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

// Ata de Resultados Finais de uma turma. Cada turma (que já é específica de um ano letivo) tem no
// máximo uma Ata — a unicidade é garantida pelo índice único em TurmaId.
public sealed class Ata : BaseEntity
{
    public Guid Id { get; set; }
    public Guid TurmaId { get; set; }

    // "Rascunho" ou "Finalizada" — ver SistemaEscolar.Application.Atas.AtaConstantes.
    public string Status { get; set; } = string.Empty;

    public DateTime? FinalizadaEmUtc { get; set; }
    public string? FinalizadaPorUserId { get; set; }
    public string? FinalizadaPorNome { get; set; }
}
