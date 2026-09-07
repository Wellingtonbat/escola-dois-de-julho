namespace SistemaEscolar.Application.Resultados;

public interface IRecuperacaoFinalRepository
{
    Task SalvarAsync(Guid alunoId, Guid disciplinaId, int anoLetivo, decimal valor, CancellationToken cancellationToken = default);
}
