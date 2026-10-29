using System.Security.Claims;
using SistemaEscolar.Application.Abstractions;

namespace SistemaEscolar.Web.Extensions;

// Atalhos para as regras de PermissoesPerfil nas páginas (Razor Pages e views). As permissões chegam como
// claims no cookie de login (uma por permissão dos perfis do usuário) e são recarregadas a cada minuto.
public static class ClaimsPrincipalExtensions
{
    public static bool TemPermissao(this ClaimsPrincipal user, string permissao) =>
        user.HasClaim(Permissoes.ClaimType, permissao);

    private static Func<string, bool> Verificador(ClaimsPrincipal user) => user.TemPermissao;

    public static bool PodeVerPainel(this ClaimsPrincipal user) => PermissoesPerfil.PodeVerPainel(Verificador(user));

    public static bool TemVisaoRestrita(this ClaimsPrincipal user) => PermissoesPerfil.TemVisaoRestrita(Verificador(user));

    public static bool PodeGerenciarAlunos(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarAlunos(Verificador(user));

    public static bool PodeGerenciarTurmas(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarTurmas(Verificador(user));

    public static bool PodeGerenciarSeries(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarSeries(Verificador(user));

    public static bool PodeGerenciarDisciplinas(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarDisciplinas(Verificador(user));

    public static bool PodeGerenciarProfessores(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarProfessores(Verificador(user));

    public static bool PodeGerenciarPeriodos(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarPeriodos(Verificador(user));

    public static bool PodeAbrirFecharPeriodo(this ClaimsPrincipal user) => PermissoesPerfil.PodeAbrirFecharPeriodo(Verificador(user));

    public static bool PodeExcluirPeriodo(this ClaimsPrincipal user) => PermissoesPerfil.PodeExcluirPeriodo(Verificador(user));

    public static bool PodeAcessarNotas(this ClaimsPrincipal user) => PermissoesPerfil.PodeAcessarNotas(Verificador(user));

    public static bool PodeAlterarNotas(this ClaimsPrincipal user) => PermissoesPerfil.PodeAlterarNotas(Verificador(user));

    public static bool PodeLancarComPeriodoFechado(this ClaimsPrincipal user) => PermissoesPerfil.PodeLancarComPeriodoFechado(Verificador(user));

    public static bool PodeBaixarBoletimPdf(this ClaimsPrincipal user) => PermissoesPerfil.PodeBaixarBoletimPdf(Verificador(user));

    public static bool PodeGerarMapaTurma(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerarMapaTurma(Verificador(user));

    public static bool PodeLancarAvaliacaoFinal(this ClaimsPrincipal user) => PermissoesPerfil.PodeLancarAvaliacaoFinal(Verificador(user));

    public static bool PodeAcessarAtas(this ClaimsPrincipal user) => PermissoesPerfil.PodeAcessarAtas(Verificador(user));

    public static bool PodeEditarAtaFinalizada(this ClaimsPrincipal user) => PermissoesPerfil.PodeEditarAtaFinalizada(Verificador(user));

    public static bool PodeGerenciarUsuarios(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarUsuarios(Verificador(user));

    public static bool PodeGerenciarPerfis(this ClaimsPrincipal user) => PermissoesPerfil.PodeGerenciarPerfis(Verificador(user));

    public static bool PodeVerAuditoria(this ClaimsPrincipal user) => PermissoesPerfil.PodeVerAuditoria(Verificador(user));
}
