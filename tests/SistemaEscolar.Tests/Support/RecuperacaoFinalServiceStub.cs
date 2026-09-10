using SistemaEscolar.Application.Resultados;

namespace SistemaEscolar.Tests.Support;

internal sealed class RecuperacaoFinalServiceStub : IRecuperacaoFinalService
{
    public Task<RecuperacaoFinalSaveResult> SalvarAsync(RecuperacaoFinalSaveRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RecuperacaoFinalSaveResult.Success());
    }

    public Task<IReadOnlyDictionary<Guid, decimal>> ListarPorAlunoEAnoAsync(Guid alunoId, int anoLetivo, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
    }

    public Task<IReadOnlyDictionary<(Guid AlunoId, Guid DisciplinaId), decimal>> ListarPorAnoAsync(int anoLetivo, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyDictionary<(Guid AlunoId, Guid DisciplinaId), decimal>>(new Dictionary<(Guid, Guid), decimal>());
    }
}
