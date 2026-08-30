namespace SistemaEscolar.Application.Disciplinas;

public interface IDisciplinaService
{
    Task<IReadOnlyList<DisciplinaListItemDto>> ListarAsync(DisciplinaListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<DisciplinaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DisciplinaCreateResult> CriarAsync(DisciplinaCreateRequest request, CancellationToken cancellationToken = default);
    Task<DisciplinaCreateResult> AtualizarAsync(Guid id, DisciplinaCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
}
