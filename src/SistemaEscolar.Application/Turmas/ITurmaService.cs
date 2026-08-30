namespace SistemaEscolar.Application.Turmas;

public interface ITurmaService
{
    Task<IReadOnlyList<TurmaListItemDto>> ListarAsync(TurmaListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<TurmaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TurmaCreateResult> CriarAsync(TurmaCreateRequest request, CancellationToken cancellationToken = default);
    Task<TurmaCreateResult> AtualizarAsync(Guid id, TurmaCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
}