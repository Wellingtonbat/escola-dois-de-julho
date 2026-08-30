namespace SistemaEscolar.Application.Periodos;

public interface IPeriodoService
{
    Task<IReadOnlyList<PeriodoListItemDto>> ListarAsync(PeriodoListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PeriodoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PeriodoListItemDto?> ObterPorAnoETrimestreAsync(int anoLetivo, int trimestre, CancellationToken cancellationToken = default);
    Task<PeriodoCreateResult> CriarAsync(PeriodoCreateRequest request, CancellationToken cancellationToken = default);
    Task<PeriodoCreateResult> AtualizarAsync(Guid id, PeriodoCreateRequest request, CancellationToken cancellationToken = default);
    Task<PeriodoCreateResult> AbrirAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PeriodoCreateResult> FecharAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
}
