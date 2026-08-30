namespace SistemaEscolar.Application.Notas;

public sealed record NotaListItemDto(
    Guid Id,
    Guid AlunoId,
    string AlunoNome,
    Guid DisciplinaId,
    string DisciplinaNome,
    Guid ProfessorId,
    string ProfessorNome,
    Guid PeriodoLancamentoId,
    string PeriodoDescricao,
    int AnoLetivo,
    int Trimestre,
    decimal? Avaliacao1,
    decimal? Avaliacao2,
    decimal? Avaliacao3,
    decimal? RecuperacaoParalela,
    decimal ResultadoUnidade,
    decimal ResultadoFinalUnidade,
    bool IsFinalizada);
