namespace SistemaEscolar.Application.Alunos;

public interface IAlunoService
{
    Task<IReadOnlyList<AlunoListItemDto>> ListarAsync(AlunoListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<AlunoCreateResult> CriarAsync(AlunoCreateRequest request, CancellationToken cancellationToken = default);
    Task<AlunoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AlunoCreateResult> AtualizarAsync(Guid id, AlunoCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
}
