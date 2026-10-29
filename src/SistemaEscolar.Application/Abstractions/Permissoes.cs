namespace SistemaEscolar.Application.Abstractions;

public sealed record PermissaoInfo(string Codigo, string Modulo, string Nome, string Descricao);

// Catálogo das permissões do sistema. Cada perfil (role do Identity) guarda as suas na tabela AspNetRoleClaims,
// e a tela "Perfis e Permissões" combina estas permissões em perfis. Uma permissão nova só existe quando o
// código passa a verificá-la, por isso o catálogo é fixo aqui e não cadastrável.
public static class Permissoes
{
    // Tipo do claim que carrega cada permissão (no banco e no cookie de login).
    public const string ClaimType = "sistemaescolar:permissao";

    // Marca, no próprio perfil, que as permissões padrão já foram gravadas. Sem ela, um perfil que o usuário
    // deixou sem nenhuma permissão voltaria a receber as padrão a cada inicialização do sistema. O valor é a
    // versão do catálogo já aplicada ao perfil (ver VersaoAtual).
    public const string ClaimTypeInicializado = "sistemaescolar:permissoes-inicializadas";

    // Sobe a cada permissão nova que deve chegar já marcada em perfis padrão existentes (ver NovasPorVersao).
    public const int VersaoAtual = 2;

    public const string PainelVer = "Painel.Ver";
    public const string EscolaVerTudo = "Escola.VerTudo";

    public const string NotasVer = "Notas.Ver";
    public const string NotasLancar = "Notas.Lancar";
    public const string NotasLancarQualquerTurma = "Notas.LancarQualquerTurma";
    public const string NotasLancarPeriodoFechado = "Notas.LancarPeriodoFechado";
    public const string BoletinsPdf = "Boletins.Pdf";
    public const string NotasMapaTurma = "Notas.MapaTurma";
    public const string AvaliacaoFinalLancar = "AvaliacaoFinal.Lancar";

    public const string PeriodosGerenciar = "Periodos.Gerenciar";
    public const string PeriodosAbrirFechar = "Periodos.AbrirFechar";

    public const string AtasGerenciar = "Atas.Gerenciar";
    public const string AtasEditarFinalizada = "Atas.EditarFinalizada";

    public const string AlunosGerenciar = "Alunos.Gerenciar";
    public const string TurmasGerenciar = "Turmas.Gerenciar";
    public const string SeriesGerenciar = "Series.Gerenciar";
    public const string DisciplinasGerenciar = "Disciplinas.Gerenciar";
    public const string ProfessoresGerenciar = "Professores.Gerenciar";

    public const string UsuariosGerenciar = "Usuarios.Gerenciar";
    public const string PerfisGerenciar = "Perfis.Gerenciar";
    public const string AuditoriaVer = "Auditoria.Ver";

    public static readonly IReadOnlyList<PermissaoInfo> Todas = new PermissaoInfo[]
    {
        new(PainelVer, "Painel e visão", "Ver o Dashboard",
            "Abre o Dashboard com os gráficos de notas."),
        new(EscolaVerTudo, "Painel e visão", "Ver a escola inteira",
            "Enxerga todas as turmas, alunos e os Cadastros. Sem esta permissão, o usuário vê apenas as turmas e disciplinas em que é professor."),

        new(NotasVer, "Notas e resultados", "Consultar notas e resultados",
            "Abre as telas de Notas, Boletim e Resultados."),
        new(NotasLancar, "Notas e resultados", "Lançar e editar notas",
            "Lança, edita, finaliza e exclui notas enquanto o período está aberto."),
        new(NotasLancarQualquerTurma, "Notas e resultados", "Lançar notas de qualquer turma",
            "Sem esta permissão, quem lança notas fica restrito às turmas e disciplinas em que é professor."),
        new(NotasLancarPeriodoFechado, "Notas e resultados", "Lançar notas com o período fechado",
            "Permite lançar e corrigir notas mesmo depois que o período foi fechado."),
        new(BoletinsPdf, "Notas e resultados", "Baixar boletins em PDF",
            "Gera o boletim do aluno e o boletim da turma em PDF."),
        new(NotasMapaTurma, "Notas e resultados", "Gerar o mapa da turma",
            "Baixa, na tela Notas, o Excel ou o PDF com as notas de todos os alunos da turma nos três trimestres."),
        new(AvaliacaoFinalLancar, "Notas e resultados", "Lançar a Avaliação Final",
            "Lança a nota da Avaliação Final na tela de Resultados."),

        new(PeriodosGerenciar, "Períodos", "Cadastrar e editar períodos",
            "Cria períodos e altera as datas. As datas definem quando o lançamento de notas fica aberto."),
        new(PeriodosAbrirFechar, "Períodos", "Abrir e fechar períodos",
            "Usa os botões Abrir e Fechar, inclusive fechando com lançamentos pendentes."),

        new(AtasGerenciar, "Atas", "Criar e editar Atas",
            "Acessa as Atas de Resultados Finais, cria, edita enquanto estão em rascunho e finaliza."),
        new(AtasEditarFinalizada, "Atas", "Reabrir Ata finalizada",
            "Usa \"Habilitar para Edição\" em uma Ata já finalizada."),

        new(AlunosGerenciar, "Cadastros", "Cadastrar e editar alunos", "Inclui ativar, inativar e excluir alunos."),
        new(TurmasGerenciar, "Cadastros", "Cadastrar e editar turmas", "Inclui ativar, inativar e excluir turmas."),
        new(SeriesGerenciar, "Cadastros", "Cadastrar e editar séries", "Inclui ativar, inativar e excluir séries."),
        new(DisciplinasGerenciar, "Cadastros", "Cadastrar e editar disciplinas", "Inclui ativar, inativar e excluir disciplinas."),
        new(ProfessoresGerenciar, "Cadastros", "Cadastrar e editar professores",
            "Inclui vínculos com turmas e disciplinas e a senha de acesso do professor."),

        new(UsuariosGerenciar, "Administração", "Gerenciar usuários",
            "Cria e edita contas, ativa, desativa e redefine senhas. Só mexe em contas cujo perfil não tenha mais permissões que as suas."),
        new(PerfisGerenciar, "Administração", "Gerenciar perfis e permissões",
            "Cria perfis e escolhe as permissões de cada um. Só concede permissões que você também tem."),
        new(AuditoriaVer, "Administração", "Consultar a auditoria",
            "Vê o histórico de quem alterou o quê no sistema."),
    };

    private static readonly Dictionary<string, PermissaoInfo> PorCodigo =
        Todas.ToDictionary(x => x.Codigo, StringComparer.Ordinal);

    public static bool Existe(string? codigo) => codigo is not null && PorCodigo.ContainsKey(codigo);

    public static PermissaoInfo? Obter(string? codigo) =>
        codigo is not null && PorCodigo.TryGetValue(codigo, out var info) ? info : null;

    public static string Descrever(string codigo) => Obter(codigo)?.Nome ?? codigo;

    private static readonly string[] Gestao =
    {
        PainelVer, EscolaVerTudo, NotasVer, BoletinsPdf, PeriodosGerenciar, AtasGerenciar,
        AlunosGerenciar, TurmasGerenciar, SeriesGerenciar, DisciplinasGerenciar, ProfessoresGerenciar, UsuariosGerenciar
    };

    // Permissões com que cada perfil já existente começa: reproduzem exatamente as regras que valiam antes
    // da tela de perfis. Só são gravadas uma vez; depois disso, quem manda é o que foi salvo na tela.
    public static IReadOnlyList<string> PadraoDoPerfil(string perfil) => perfil switch
    {
        Perfis.Diretor or Perfis.ViceDiretor => Todas.Select(x => x.Codigo).ToList(),
        Perfis.Coordenador or Perfis.CoordenadorLegado => Gestao.Append(PeriodosAbrirFechar).Append(NotasMapaTurma).ToList(),
        Perfis.Secretaria => Gestao,
        Perfis.Professor => new[] { PainelVer, NotasVer, NotasLancar },
        _ => Array.Empty<string>()
    };

    // Permissões criadas depois da primeira versão e os perfis padrão que já as recebem marcadas. Na
    // inicialização, um perfil que está numa versão anterior ganha só estas, sem perder o que foi configurado na
    // tela. Diretor e Vice-Diretor não precisam constar: têm sempre todas.
    private static readonly Dictionary<int, Dictionary<string, string[]>> NovasPorVersao = new()
    {
        [2] = new(StringComparer.Ordinal)
        {
            [Perfis.Coordenador] = new[] { NotasMapaTurma },
            [Perfis.CoordenadorLegado] = new[] { NotasMapaTurma },
        },
    };

    public static IReadOnlyList<string> NovasDesdeVersao(int versaoDoPerfil, string perfil) =>
        NovasPorVersao
            .Where(x => x.Key > versaoDoPerfil)
            .SelectMany(x => x.Value.GetValueOrDefault(perfil) ?? Array.Empty<string>())
            .Distinct()
            .ToList();
}
