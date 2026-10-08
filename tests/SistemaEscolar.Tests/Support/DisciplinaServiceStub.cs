using SistemaEscolar.Application.Disciplinas;

namespace SistemaEscolar.Tests.Support;

internal sealed class DisciplinaServiceStub : IDisciplinaService
{
    private readonly IReadOnlyList<DisciplinaListItemDto> _disciplinas;

    public DisciplinaServiceStub(IReadOnlyList<DisciplinaListItemDto>? disciplinas = null)
    {
        _disciplinas = disciplinas ?? Array.Empty<DisciplinaListItemDto>();
    }

    public Task<IReadOnlyList<DisciplinaListItemDto>> ListarAsync(DisciplinaListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_disciplinas);

    public Task<DisciplinaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<DisciplinaListItemDto?>(null);

    public Task<DisciplinaCreateResult> CriarAsync(DisciplinaCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(DisciplinaCreateResult.Fail("not implemented in test stub"));

    public Task<DisciplinaCreateResult> AtualizarAsync(Guid id, DisciplinaCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(DisciplinaCreateResult.Fail("not implemented in test stub"));

    public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
