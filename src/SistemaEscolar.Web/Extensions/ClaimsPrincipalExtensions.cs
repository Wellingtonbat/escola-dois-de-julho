using System.Security.Claims;
using SistemaEscolar.Application.Abstractions;

namespace SistemaEscolar.Web.Extensions;

// Atalhos para as regras de PermissoesPerfil nas páginas (Razor Pages e views), para que nenhuma
// página precise repetir listas de perfis.
public static class ClaimsPrincipalExtensions
{
    public static bool EhDiretoria(this ClaimsPrincipal user) => PermissoesPerfil.EhDiretoria(user.IsInRole);

    public static bool EhCoordenacao(this ClaimsPrincipal user) => PermissoesPerfil.EhCoordenacao(user.IsInRole);

    public static bool EhGestao(this ClaimsPrincipal user) => PermissoesPerfil.EhGestao(user.IsInRole);

    public static bool EhApenasProfessor(this ClaimsPrincipal user) => PermissoesPerfil.EhApenasProfessor(user.IsInRole);

    public static bool PodeGerenciarContaComPerfil(this ClaimsPrincipal user, string perfil) =>
        PermissoesPerfil.PodeGerenciarContaComPerfil(user.IsInRole, perfil);

    public static bool PodeVerAuditoria(this ClaimsPrincipal user) => PermissoesPerfil.PodeVerAuditoria(user.IsInRole);

    public static bool PodeAbrirFecharPeriodo(this ClaimsPrincipal user) => PermissoesPerfil.PodeAbrirFecharPeriodo(user.IsInRole);

    public static bool PodeExcluirPeriodo(this ClaimsPrincipal user) => PermissoesPerfil.PodeExcluirPeriodo(user.IsInRole);

    public static bool PodeAcessarNotas(this ClaimsPrincipal user) => PermissoesPerfil.PodeAcessarNotas(user.IsInRole);

    public static bool PodeAlterarNotas(this ClaimsPrincipal user) => PermissoesPerfil.PodeAlterarNotas(user.IsInRole);
}
