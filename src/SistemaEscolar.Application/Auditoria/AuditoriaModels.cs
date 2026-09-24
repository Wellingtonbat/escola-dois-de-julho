namespace SistemaEscolar.Application.Auditoria;

// Filtros da tela de Auditoria. As datas são informadas no horário de Brasília (dia inteiro, inclusive a data final).
public sealed record AuditoriaFiltro(
    DateTime? DataInicial,
    DateTime? DataFinal,
    string? Usuario,
    string? Entidade,
    string? Acao,
    int Pagina,
    int TamanhoPagina);

public sealed record AuditoriaAlteracaoDto(string Campo, string? ValorAnterior, string? ValorNovo);

public sealed record AuditoriaItemDto(
    long Id,
    DateTime DataHoraLocal,
    string Usuario,
    string? UsuarioLogin,
    string Entidade,
    string Acao,
    string ClasseAcao,
    string Registro,
    IReadOnlyList<AuditoriaAlteracaoDto> Alteracoes);

public sealed record AuditoriaPaginaDto(
    IReadOnlyList<AuditoriaItemDto> Itens,
    int Pagina,
    int TotalPaginas,
    int Total);

public sealed record AuditoriaEntidadeDto(string Tabela, string Rotulo);

public sealed record AuditoriaListResult(bool Succeeded, string? ErrorMessage, AuditoriaPaginaDto? Pagina)
{
    public static AuditoriaListResult Success(AuditoriaPaginaDto pagina) => new(true, null, pagina);

    public static AuditoriaListResult Fail(string errorMessage) => new(false, errorMessage, null);
}
