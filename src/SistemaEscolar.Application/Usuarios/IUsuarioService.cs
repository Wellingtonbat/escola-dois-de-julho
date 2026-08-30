namespace SistemaEscolar.Application.Usuarios;

public interface IUsuarioService
{
    IReadOnlyList<string> PerfisDisponiveis { get; }

    Task<IReadOnlyList<UsuarioListItemDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<UsuarioListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UsuarioCreateResult> CriarAsync(UsuarioCreateRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioCreateResult> AtualizarAsync(Guid id, UsuarioCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UsuarioCreateResult> RedefinirSenhaAsync(Guid id, string novaSenha, CancellationToken cancellationToken = default);
}
