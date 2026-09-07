using SistemaEscolar.Application.Resultados;

namespace SistemaEscolar.Tests.Support;

internal sealed class RecuperacaoFinalServiceStub : IRecuperacaoFinalService
{
    public Task<RecuperacaoFinalSaveResult> SalvarAsync(RecuperacaoFinalSaveRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RecuperacaoFinalSaveResult.Success());
    }
}
