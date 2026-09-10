namespace SistemaEscolar.Application.Resultados;

public interface IRecuperacaoFinalRepository
{
    Task SalvarAsync(Guid alunoId, Guid disciplinaId, int anoLetivo, decimal valor, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> ListarPorAlunoEAnoAsync(Guid alunoId, int anoLetivo, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<(Guid AlunoId, Guid DisciplinaId), decimal>> ListarPorAnoAsync(int anoLetivo, CancellationToken cancellationToken = default);
}
