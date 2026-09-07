namespace SistemaEscolar.Application.Resultados;

public interface IRecuperacaoFinalService
{
    Task<RecuperacaoFinalSaveResult> SalvarAsync(RecuperacaoFinalSaveRequest request, CancellationToken cancellationToken = default);
}
