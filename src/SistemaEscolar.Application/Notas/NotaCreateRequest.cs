namespace SistemaEscolar.Application.Notas;

public sealed record NotaCreateRequest(
    Guid AlunoId,
    Guid DisciplinaId,
    Guid ProfessorId,
    Guid PeriodoLancamentoId,
    decimal? Avaliacao1,
    decimal? Avaliacao2,
    decimal? Avaliacao3,
    decimal? RecuperacaoParalela,
    bool IsFinalizada);
