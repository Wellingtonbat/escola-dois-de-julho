namespace SistemaEscolar.Application.Abstractions;

// Fonte única das regras de permissão. Recebe uma função "temPermissao" para poder ser usada tanto pelas
// páginas (ClaimsPrincipal.TemPermissao) quanto pelos serviços (ICurrentUserService.TemPermissao).
// As permissões de cada perfil são configuradas na tela "Perfis e Permissões" (ver Permissoes); como um
// usuário pode acumular perfis, ele tem a soma das permissões de todos eles.
public static class PermissoesPerfil
{
    public static bool PodeVerPainel(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.PainelVer);

    // Visão restrita: o usuário só enxerga as turmas e disciplinas em que é professor e não vê os Cadastros.
    // É o caso do Professor; quem acumula Professor + um perfil de gestão enxerga a escola inteira.
    public static bool TemVisaoRestrita(Func<string, bool> temPermissao) =>
        !temPermissao(Permissoes.EscolaVerTudo);

    public static bool PodeGerenciarAlunos(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.AlunosGerenciar);

    public static bool PodeGerenciarTurmas(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.TurmasGerenciar);

    public static bool PodeGerenciarSeries(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.SeriesGerenciar);

    public static bool PodeGerenciarDisciplinas(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.DisciplinasGerenciar);

    public static bool PodeGerenciarProfessores(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.ProfessoresGerenciar);

    public static bool PodeGerenciarPeriodos(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.PeriodosGerenciar);

    public static bool PodeAbrirFecharPeriodo(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.PeriodosAbrirFechar);

    // Exclusão de períodos desativada para todos os perfis: um período carrega as notas do trimestre, e a escola
    // passou a fechar o período (com a lista de pendências) em vez de excluí-lo.
    public static bool PodeExcluirPeriodo(Func<string, bool> temPermissao) => false;

    // Abrir as telas de Notas, Boletim e Resultados. Quem lança notas, lança a Avaliação Final ou gera o mapa
    // da turma precisa dessas telas para trabalhar, então essas permissões também dão acesso de consulta.
    public static bool PodeAcessarNotas(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.NotasVer)
        || temPermissao(Permissoes.NotasLancar)
        || temPermissao(Permissoes.AvaliacaoFinalLancar)
        || temPermissao(Permissoes.NotasMapaTurma);

    // Lançar, editar, finalizar ou excluir notas. Com o período fechado, só quem tem
    // PodeLancarComPeriodoFechado; fora das próprias turmas, só quem tem "lançar notas de qualquer turma".
    public static bool PodeAlterarNotas(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.NotasLancar);

    public static bool PodeLancarComPeriodoFechado(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.NotasLancarPeriodoFechado);

    // Quem lança notas sem a permissão "lançar notas de qualquer turma" fica restrito às próprias turmas e
    // disciplinas. Vale mesmo para quem acumula Professor + Coordenador/Secretária, para que o perfil de
    // gestão não sirva de atalho para editar notas de outros professores.
    public static bool AlteracaoDeNotasRestritaAoEscopo(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.NotasLancar) && !temPermissao(Permissoes.NotasLancarQualquerTurma);

    public static bool PodeBaixarBoletimPdf(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.BoletinsPdf);

    // Mapa de notas da turma (Excel/PDF) na tela Notas. Quem tem visão restrita só gera das próprias turmas e
    // disciplinas.
    public static bool PodeGerarMapaTurma(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.NotasMapaTurma);

    // Avaliação Final (antiga "Recuperação Final", lançada em Resultados).
    public static bool PodeLancarAvaliacaoFinal(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.AvaliacaoFinalLancar);

    // Ata de Resultados Finais: acessar a lista, criar uma Ata, editar enquanto está em Rascunho e finalizar.
    public static bool PodeAcessarAtas(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.AtasGerenciar);

    // Depois que uma Ata é finalizada, voltar a editá-la (via "Habilitar para Edição").
    public static bool PodeEditarAtaFinalizada(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.AtasEditarFinalizada);

    public static bool PodeGerenciarUsuarios(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.UsuariosGerenciar);

    public static bool PodeGerenciarPerfis(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.PerfisGerenciar);

    // Auditoria (quem alterou o quê) expõe dados de todos os usuários.
    public static bool PodeVerAuditoria(Func<string, bool> temPermissao) =>
        temPermissao(Permissoes.AuditoriaVer);

    // Regra contra escalonamento de acesso: só se concede, altera ou administra o que você mesmo já tem.
    // Usada para contas (não mexer na conta de quem tem mais permissões que você) e para perfis (não conceder
    // uma permissão que você não tem).
    public static bool CobrePermissoes(Func<string, bool> temPermissao, IEnumerable<string> permissoes) =>
        permissoes.All(temPermissao);
}
