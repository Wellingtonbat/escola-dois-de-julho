using SistemaEscolar.Application.Professores;

namespace SistemaEscolar.Tests.Support;

internal sealed class ProfessorServiceStub : IProfessorService
{
    private readonly IReadOnlyList<ProfessorListItemDto> _professores;

    public ProfessorServiceStub(IReadOnlyList<ProfessorListItemDto>? professores = null)
    {
        _professores = professores ?? Array.Empty<ProfessorListItemDto>();
    }

    public Task<IReadOnlyList<ProfessorListItemDto>> ListarAsync(ProfessorListFilter? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_professores);

    public Task<ProfessorCreateResult> CriarAsync(ProfessorCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(ProfessorCreateResult.Fail("not implemented in test stub"));

    public Task<ProfessorListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProfessorListItemDto?>(null);

    public Task<ProfessorCreateResult> AtualizarAsync(Guid id, ProfessorCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(ProfessorCreateResult.Fail("not implemented in test stub"));

    public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<ProfessorEscopoDto?> ObterEscopoPorUsuarioAsync(string? userName, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProfessorEscopoDto?>(null);

    public Task<ProfessorCreateResult> RedefinirSenhaAsync(Guid id, string novaSenha, CancellationToken cancellationToken = default) =>
        Task.FromResult(ProfessorCreateResult.Fail("not implemented in test stub"));

    public Task<IReadOnlySet<string>> ListarCpfsViceDiretoresAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
}
