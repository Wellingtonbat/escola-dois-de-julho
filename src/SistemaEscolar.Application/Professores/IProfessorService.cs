namespace SistemaEscolar.Application.Professores;

public interface IProfessorService
{
    Task<IReadOnlyList<ProfessorListItemDto>> ListarAsync(ProfessorListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<ProfessorCreateResult> CriarAsync(ProfessorCreateRequest request, CancellationToken cancellationToken = default);
    Task<ProfessorListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProfessorCreateResult> AtualizarAsync(Guid id, ProfessorCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProfessorEscopoDto?> ObterEscopoPorUsuarioAsync(string? userName, CancellationToken cancellationToken = default);
    Task<ProfessorCreateResult> RedefinirSenhaAsync(Guid id, string novaSenha, CancellationToken cancellationToken = default);

    // CPFs dos professores que também têm o perfil de Vice-Diretor (para sinalizar na listagem).
    Task<IReadOnlySet<string>> ListarCpfsViceDiretoresAsync(CancellationToken cancellationToken = default);
}
