using SistemaEscolar.Application.Alunos;

namespace SistemaEscolar.Tests.Support;

internal sealed class AlunoServiceStub : IAlunoService
{
    private readonly IReadOnlyList<AlunoListItemDto> _alunos;

    public AlunoServiceStub(IReadOnlyList<AlunoListItemDto>? alunos = null)
    {
        _alunos = alunos ?? Array.Empty<AlunoListItemDto>();
    }

    public Task<IReadOnlyList<AlunoListItemDto>> ListarAsync(AlunoListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_alunos);

    public Task<AlunoCreateResult> CriarAsync(AlunoCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(AlunoCreateResult.Fail("not implemented in test stub"));

    public Task<AlunoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<AlunoListItemDto?>(null);

    public Task<AlunoCreateResult> AtualizarAsync(Guid id, AlunoCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(AlunoCreateResult.Fail("not implemented in test stub"));

    public Task<AlunoCreateResult> AtualizarMatriculaPrefeituraAsync(Guid id, string? matriculaPrefeitura, CancellationToken cancellationToken = default) =>
        Task.FromResult(AlunoCreateResult.Fail("not implemented in test stub"));

    public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
