using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class Nota : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AlunoId { get; set; }
    public Guid DisciplinaId { get; set; }
    public Guid ProfessorId { get; set; }
    public Guid PeriodoLancamentoId { get; set; }
    public decimal? Avaliacao1 { get; set; }
    public decimal? Avaliacao2 { get; set; }
    public decimal? Avaliacao3 { get; set; }
    public decimal? RecuperacaoParalela { get; set; }
    public decimal ResultadoUnidade { get; set; }
    public decimal ResultadoFinalUnidade { get; set; }
    public decimal Valor { get; set; }
    public bool IsFinalizada { get; set; }
}
