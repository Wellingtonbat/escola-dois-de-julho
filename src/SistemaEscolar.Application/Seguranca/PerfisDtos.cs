namespace SistemaEscolar.Application.Seguranca;

public sealed record PerfilListItemDto(
    Guid Id,
    string Nome,
    string NomeExibicao,
    // Perfil que já vem com o sistema (o código depende do nome dele): não pode ser renomeado nem excluído.
    bool EhPadrao,
    // Diretor: sempre com todas as permissões, garante que a escola nunca fique sem um perfil com acesso total.
    bool EhSomenteLeitura,
    int TotalUsuarios,
    IReadOnlyList<string> Permissoes,
    bool PodeEditar,
    bool PodeExcluir);

public sealed record PerfilDetalheDto(PerfilListItemDto Perfil, IReadOnlyList<string> Usuarios);

public sealed record PerfilOpcaoDto(string Nome, string NomeExibicao);

public sealed record PerfilSalvarRequest(string? Nome, IReadOnlyCollection<string>? Permissoes);

public sealed record PerfilResult(bool Succeeded, string? ErrorMessage, Guid? Id = null)
{
    public static PerfilResult Success(Guid id) => new(true, null, id);

    public static PerfilResult Fail(string errorMessage) => new(false, errorMessage);
}

public interface IPerfilService
{
    Task<IReadOnlyList<PerfilListItemDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<PerfilDetalheDto?> ObterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PerfilResult> CriarAsync(PerfilSalvarRequest request, CancellationToken cancellationToken = default);

    Task<PerfilResult> AtualizarAsync(Guid id, PerfilSalvarRequest request, CancellationToken cancellationToken = default);

    Task<PerfilResult> ExcluirAsync(Guid id, CancellationToken cancellationToken = default);

    // Verdadeiro quando o usuário logado tem todas as permissões dos perfis informados. É a regra contra
    // escalonamento usada por Usuários e Professores: não se mexe na conta de quem tem mais acesso que você.
    Task<bool> UsuarioAtualCobrePerfisAsync(IEnumerable<string> perfis, CancellationToken cancellationToken = default);
}
