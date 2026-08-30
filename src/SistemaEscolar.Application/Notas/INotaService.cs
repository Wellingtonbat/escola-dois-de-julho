namespace SistemaEscolar.Application.Notas;

public interface INotaService
{
    Task<IReadOnlyList<NotaListItemDto>> ListarAsync(NotaListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<NotaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotaCreateResult> CriarAsync(NotaCreateRequest request, CancellationToken cancellationToken = default);
    Task<NotaCreateResult> AtualizarAsync(Guid id, NotaCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> AlternarFinalizacaoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
}
