namespace SistemaEscolar.Application.Resultados;

public interface IRecuperacaoFinalService
{
    Task<RecuperacaoFinalSaveResult> SalvarAsync(RecuperacaoFinalSaveRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> ListarPorAlunoEAnoAsync(Guid alunoId, int anoLetivo, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<(Guid AlunoId, Guid DisciplinaId), decimal>> ListarPorAnoAsync(int anoLetivo, CancellationToken cancellationToken = default);
}
