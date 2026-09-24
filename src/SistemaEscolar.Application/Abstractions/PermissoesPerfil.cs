namespace SistemaEscolar.Application.Abstractions;

// Fonte única das regras de permissão por perfil. Recebe uma função "isInRole" para poder ser usada
// tanto pelas páginas (ClaimsPrincipal.IsInRole) quanto pelos serviços (ICurrentUserService.IsInRole).
// Como os perfis se acumulam, toda regra é escrita em termos de "tem algum destes perfis".
public static class PermissoesPerfil
{
    // Diretor e Vice-Diretor têm exatamente as mesmas permissões.
    public static bool EhDiretoria(Func<string, bool> isInRole) =>
        isInRole(Perfis.Diretor) || isInRole(Perfis.ViceDiretor);

    public static bool EhCoordenacao(Func<string, bool> isInRole) =>
        isInRole(Perfis.Coordenador) || isInRole(Perfis.CoordenadorLegado);

    // Qualquer perfil administrativo (cadastros, painel completo, gestão de usuários...).
    public static bool EhGestao(Func<string, bool> isInRole) =>
        EhDiretoria(isInRole) || EhCoordenacao(isInRole) || isInRole(Perfis.Secretaria);

    // Professor "puro": usuário que só tem o perfil Professor, sem nenhum perfil de gestão.
    // Quem acumula Professor + gestão (ex.: professor que é vice-diretor) enxerga o sistema como gestão.
    public static bool EhApenasProfessor(Func<string, bool> isInRole) =>
        isInRole(Perfis.Professor) && !EhGestao(isInRole);

    // Contas de Diretor e Vice-Diretor só podem ser criadas, alteradas ou removidas pela própria Diretoria.
    // Sem isso, um perfil de gestão menor poderia se conceder (ou conceder a outro usuário) as permissões
    // de Diretoria por meio da tela de Usuários ou do marcador de Vice-Diretor em Professores.
    public static bool PodeGerenciarContaComPerfil(Func<string, bool> isInRole, string perfil) =>
        perfil is Perfis.Diretor or Perfis.ViceDiretor
            ? EhDiretoria(isInRole)
            : EhGestao(isInRole);

    // Auditoria (quem alterou o quê) é restrita à Diretoria.
    public static bool PodeVerAuditoria(Func<string, bool> isInRole) =>
        EhDiretoria(isInRole);

    public static bool PodeAbrirFecharPeriodo(Func<string, bool> isInRole) =>
        EhDiretoria(isInRole) || EhCoordenacao(isInRole);

    public static bool PodeExcluirPeriodo(Func<string, bool> isInRole) =>
        EhDiretoria(isInRole);

    // Ver a lista de notas, boletins e resultados (consulta).
    public static bool PodeAcessarNotas(Func<string, bool> isInRole) =>
        EhGestao(isInRole) || isInRole(Perfis.Professor);

    // Lançar, editar, finalizar ou excluir notas. Diretoria em qualquer período; Professor apenas com
    // o período aberto e nas suas turmas/disciplinas (isso é verificado à parte). Coordenador e
    // Secretária têm acesso somente de consulta.
    public static bool PodeAlterarNotas(Func<string, bool> isInRole) =>
        EhDiretoria(isInRole) || isInRole(Perfis.Professor);

    // Quem altera notas como Professor fica restrito às próprias turmas e disciplinas, a não ser que
    // também seja Diretoria. Vale mesmo para quem acumula Professor + Coordenador/Secretária, para que
    // o perfil de gestão não sirva de atalho para editar notas de outros professores.
    public static bool AlteracaoDeNotasRestritaAoEscopo(Func<string, bool> isInRole) =>
        isInRole(Perfis.Professor) && !EhDiretoria(isInRole);
}
