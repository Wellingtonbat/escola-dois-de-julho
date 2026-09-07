namespace SistemaEscolar.Application.Resultados;

public sealed record RecuperacaoFinalSaveRequest(
    Guid AlunoId,
    Guid DisciplinaId,
    int AnoLetivo,
    decimal Valor);
