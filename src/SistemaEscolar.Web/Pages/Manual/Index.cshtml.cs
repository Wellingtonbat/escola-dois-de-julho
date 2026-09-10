using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaEscolar.Web.Pages.Manual;

public sealed class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public IndexModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public void OnGet()
    {
    }

    public IActionResult OnGetDownloadPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var appVersion = System.Reflection.Assembly.GetEntryAssembly()?
            .GetName()
            .Version?
            .ToString(3) ?? "1.0.0";

        var imagens = CarregarImagens();

        var content = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10.5f));

                page.Header()
                    .PaddingBottom(6)
                    .Text("Manual de Utilização - Sistema Escolar")
                    .SemiBold()
                    .FontSize(15)
                    .FontColor(Colors.Blue.Darken1);

                page.Content().Column(column =>
                {
                    column.Spacing(6);

                    // Capa
                    column.Item().Text($"Versão da aplicação: {appVersion}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    column.Item().Text($"Data de emissão: {DateTime.Now:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Darken1);

                    Paragrafo(column,
                        "Este manual explica, de um jeito bem simples, como usar o Sistema Escolar. " +
                        "Não se preocupe se você nunca usou o sistema antes: vamos explicar cada tela como se " +
                        "estivéssemos mostrando pessoalmente, com direito a fotos de cada parte!");

                    Titulo(column, "1. O que é o Sistema Escolar?");
                    Paragrafo(column,
                        "Pense no Sistema Escolar como um caderno gigante e organizado da escola, só que dentro do computador. " +
                        "Nele ficam guardados os nomes dos alunos, as turmas, as disciplinas, os professores e as notas de cada trimestre. " +
                        "Em vez de folhear páginas de papel, você clica em telas — e o sistema faz as contas de média sozinho!");

                    Titulo(column, "2. Como entrar no sistema (Login)");
                    Paragrafo(column,
                        "Para entrar, você precisa de duas coisas: o seu CPF e uma senha. É a mesma ideia de entrar no e-mail ou em um " +
                        "aplicativo de celular: sem essas duas informações certas, o sistema não deixa ninguém entrar — isso protege os dados dos alunos.");
                    Bullet(column, "Digite o seu CPF no campo \"CPF\".");
                    Bullet(column, "Digite sua senha no campo \"Senha\".");
                    Bullet(column, "Clique no botão azul \"Entrar\".");
                    Paragrafo(column, "Se você errar o CPF ou a senha, o sistema mostra uma mensagem vermelha avisando e pede para tentar de novo.");
                    Print(column, imagens["login"], "Tela de login: é a porta de entrada do sistema.");

                    column.Item().PageBreak();
                    Titulo(column, "3. Quem pode fazer o quê? (Perfis de acesso)");
                    Paragrafo(column,
                        "Cada pessoa que usa o sistema tem um \"crachá\" chamado perfil. O crachá decide quais portas (telas) essa " +
                        "pessoa pode abrir. Existem 4 crachás diferentes:");
                    Bullet(column, "Diretor(a) — o crachá mais completo. Pode fazer absolutamente tudo no sistema, inclusive coisas que mais ninguém pode, como abrir e fechar um trimestre.");
                    Bullet(column, "Coordenador(a) — quase tão completo quanto o do Diretor. Cuida do dia a dia acadêmico (alunos, turmas, professores, notas), mas não pode abrir nem fechar trimestres.");
                    Bullet(column, "Secretária — tem exatamente as mesmas permissões do Coordenador: cuida dos cadastros e das notas, mas também não abre nem fecha trimestres.");
                    Bullet(column, "Professor(a) — o crachá mais simples. Só enxerga as suas próprias turmas e disciplinas, e só pode lançar as notas dos alunos que ele mesmo dá aula.");
                    Paragrafo(column,
                        "Guarde essa ideia: Diretor, Coordenador e Secretária enxergam o sistema quase do mesmo jeito (menu completo). " +
                        "Já o Professor enxerga um sistema bem mais enxuto, com menos botões — isso é de propósito, para simplificar o dia a dia dele.");

                    Titulo(column, "4. O Painel Inicial (Dashboard)");
                    Paragrafo(column,
                        "É a primeira tela que você vê depois de entrar. Para o Diretor, o Coordenador e a Secretária, ela é um painel " +
                        "de controle bem completo e colorido: lá em cima ficam os filtros — Professor, Turma, Disciplina e Ano/Trimestre. " +
                        "Escolha o que quiser conferir e clique em \"Filtrar\": todos os gráficos da tela se atualizam na hora, sem precisar " +
                        "recarregar a página.");
                    Bullet(column, "Cartões coloridos no topo — sempre mostram números da escola inteira: quantos alunos, o percentual geral de aprovação, quantas notas ainda estão pendentes de lançamento e quantos trimestres estão abertos.");
                    Bullet(column, "Gráfico \"Aprovados x Reprovados x Pendentes\" — um gráfico de rosca que mostra a proporção de cada situação, considerando os filtros escolhidos.");
                    Bullet(column, "Gráfico \"Média por disciplina\" — aparece depois que você escolhe uma turma, comparando a média de cada matéria dentro dela.");
                    Bullet(column, "Gráfico \"Evolução por trimestre\" — mostra se a média está subindo ou caindo ao longo do ano.");
                    Bullet(column, "Ranking de turmas — lista lado a lado as 5 turmas com melhor e as 5 com pior índice de aprovação.");
                    Bullet(column, "Mapa de pendências — uma tabela colorida que cruza turma com disciplina, mostrando de relance onde ainda faltam notas para lançar.");
                    Paragrafo(column, "Já para o Professor, o Painel Inicial continua bem mais simples, como você verá na seção 15.");
                    Print(column, imagens["dashboard-admin"], "Painel Inicial visto por Diretor, Coordenador ou Secretária, com uma turma selecionada nos filtros.");

                    column.Item().PageBreak();
                    Titulo(column, "5. Alunos");
                    Paragrafo(column,
                        "Aqui ficam cadastrados todos os alunos da escola: nome, CPF, data de nascimento, série e turma. " +
                        "Diretor, Coordenador e Secretária podem cadastrar, editar, ativar/desativar e excluir alunos. " +
                        "Também é possível baixar um modelo de planilha e importar uma lista inteira de alunos de uma vez.");
                    Print(column, imagens["alunos"], "Tela de Alunos: lista, filtros e ações de cadastro.");

                    Titulo(column, "6. Disciplinas");
                    Paragrafo(column,
                        "São as matérias da escola, como Matemática, História, Português. Cada disciplina pode ser vinculada a uma ou mais séries.");
                    Print(column, imagens["disciplinas"], "Tela de Disciplinas.");

                    column.Item().PageBreak();
                    Titulo(column, "7. Professores");
                    Paragrafo(column,
                        "Aqui ficam os dados dos professores e, o mais importante, quais turmas e disciplinas cada um leciona. " +
                        "É esse vínculo que decide o que aparece para o professor quando ele entra no sistema com o próprio login.");
                    Print(column, imagens["professores"], "Tela de Professores.");

                    Titulo(column, "8. Turmas e Séries");
                    Paragrafo(column,
                        "Série é o \"ano\" (exemplo: 8º Ano). Turma é a divisão dentro da série (exemplo: 8º Ano A, 8º Ano B). " +
                        "Cada turma pertence a uma série.");
                    Print(column, imagens["turmas"], "Tela de Turmas.");
                    Print(column, imagens["series"], "Tela de Séries.");

                    column.Item().PageBreak();
                    Titulo(column, "9. Períodos de Lançamento");
                    Paragrafo(column,
                        "Um período representa um trimestre de um ano letivo (exemplo: \"2º Trimestre 2026\"). Só é possível lançar notas " +
                        "dentro de um período que já foi cadastrado. Cada período também tem um status: Aberto (dá para lançar/editar notas) " +
                        "ou Fechado (as notas ficam travadas).");
                    Paragrafo(column,
                        "Aqui está a primeira grande diferença entre os perfis: só o Diretor tem os botões \"Abrir\" e \"Fechar\" período. " +
                        "Coordenador e Secretária conseguem cadastrar e editar períodos, mas não conseguem trocar o status de aberto/fechado " +
                        "— compare as duas fotos abaixo, tiradas da mesma tela, com perfis diferentes:");
                    Print(column, imagens["periodos-diretor"], "Períodos visto pelo Diretor: repare nos botões \"Abrir\"/\"Fechar\".");
                    Print(column, imagens["periodos-coordenador"], "A mesma tela vista por um Coordenador: os botões \"Abrir\"/\"Fechar\" não existem.");

                    column.Item().PageBreak();
                    Titulo(column, "10. Notas");
                    Paragrafo(column,
                        "É onde as notas de cada aluno, em cada disciplina e período, são lançadas. Para cada lançamento existem até 4 números:");
                    Bullet(column, "Avaliação 1, Avaliação 2 e Avaliação 3 — as três provas/atividades do trimestre (de 0 a 10).");
                    Bullet(column, "Recuperação Paralela — uma nota extra, opcional, para quem precisa recuperar.");
                    Paragrafo(column, "O sistema faz as contas sozinho, seguindo esta receita:");
                    Bullet(column, "Resultado da Unidade = a média das 3 avaliações.");
                    Bullet(column, "Resultado Final da Unidade = o maior valor entre o Resultado da Unidade e a Recuperação Paralela (quando ela existir).");
                    Paragrafo(column,
                        "Importante: se o período estiver Fechado, somente o Diretor consegue editar ou excluir uma nota já lançada nele. " +
                        "Os outros perfis conseguem apenas consultar.");
                    Print(column, imagens["notas-diretor"], "Tela de Notas vista pelo Diretor: mostra os lançamentos de todos os professores.");

                    column.Item().PageBreak();
                    Titulo(column, "11. Boletim do Aluno e Lançamento de Notas em Massa");
                    Paragrafo(column,
                        "Clicando em qualquer lançamento da lista de Notas, você abre o Boletim completo daquele aluno: todas as " +
                        "disciplinas do ano, lado a lado, com a situação (Aprovado, Reprovado ou Em andamento) em cada uma. É a forma " +
                        "mais rápida de ver — ou lançar — todas as notas de um único aluno de uma vez só.");
                    Print(column, imagens["boletim-aluno"], "Boletim do Aluno: todas as disciplinas do ano, com a situação em cada uma.");
                    Paragrafo(column,
                        "Se você é Diretor, Coordenador ou Secretária, o botão \"Baixar Boletim (PDF)\" gera esse boletim no mesmo modelo " +
                        "impresso usado pela escola — pronto para imprimir ou anexar em processos.");
                    Paragrafo(column,
                        "E quando é preciso lançar a nota de uma turma inteira, aluno por aluno, um de cada vez fica trabalhoso. Por isso " +
                        "existe o botão \"Lançamento em Massa\", no topo da tela de Notas: você escolhe o trimestre, a turma e a disciplina " +
                        "uma única vez, e o sistema abre uma planilha com todos os alunos daquela turma, um em cada linha.");
                    Bullet(column, "Digite as notas e aperte Tab para pular de campo em campo — cada nota é salva sozinha assim que você sai do campo, sem precisar clicar em nenhum botão de salvar.");
                    Bullet(column, "A coluna \"Status\" muda de cor na hora, mostrando se aquele aluno já está Aprovado, Reprovado ou ainda Não lançado.");
                    Bullet(column, "O Professor só enxerga, também aqui, as turmas e disciplinas que ele mesmo leciona — a mesma regra de sempre.");
                    Print(column, imagens["notas-lancamento-massa"], "Lançamento em Massa: uma planilha com todos os alunos da turma, notas salvando sozinhas.");

                    column.Item().PageBreak();
                    Titulo(column, "13. Resultados");
                    Paragrafo(column,
                        "É o boletim final. Junta as notas de todas as disciplinas de um aluno no ano e mostra a situação dele:");
                    Bullet(column, "Aprovado — média final igual ou maior que 5,0, com todas as notas do ano lançadas.");
                    Bullet(column, "Reprovado — média final menor que 5,0, mesmo depois de considerar a Recuperação Final (quando existir).");
                    Bullet(column, "Pendente — ainda falta lançar alguma nota do ano; o sistema ainda não consegue calcular o resultado final.");
                    Paragrafo(column,
                        "Quando um aluno fica com média abaixo de 5,0 em alguma disciplina, esta tela mostra um campinho para lançar a " +
                        "\"Recuperação Final\" dele — a última chance de recuperar no ano. Se a nota da recuperação for maior que a média, " +
                        "ela passa a valer, e a situação do aluno muda automaticamente para Aprovado.");
                    Paragrafo(column,
                        "Nesta tela também dá para filtrar por turma, série e situação, buscar por aluno, disciplina ou professor, e " +
                        "exportar a lista em CSV, Excel (XLSX) ou PDF. Na exportação em Excel, cada disciplina ganha sua própria aba, " +
                        "já no mesmo formato de planilha usado pela escola.");
                    Print(column, imagens["resultados-diretor"], "Tela de Resultados vista pelo Diretor: todos os alunos da escola.");

                    column.Item().PageBreak();
                    Titulo(column, "14. Usuários (só para Diretor, Coordenador e Secretária)");
                    Paragrafo(column,
                        "Essa tela serve para criar os logins de Diretor, Coordenador e Secretária (os logins de Professor são criados " +
                        "automaticamente lá na tela de Professores). Quem cria um usuário novo escolhe uma senha padrão, e o sistema obriga " +
                        "a pessoa a trocar essa senha assim que ela entrar pela primeira vez — é uma proteção extra.");
                    Print(column, imagens["usuarios"], "Tela de Usuários: cadastro de Diretor, Coordenador e Secretária.");
                    Print(column, imagens["trocar-senha"], "Tela mostrada automaticamente no primeiro acesso, obrigando a criar uma nova senha.");

                    column.Item().PageBreak();
                    Titulo(column, "15. O que o Professor enxerga (visão simplificada)");
                    Paragrafo(column,
                        "Quando um Professor entra no sistema, o menu lateral aparece bem mais curto: só Dashboard, Notas, Resultados e o botão " +
                        "para baixar este manual. Ele não vê Alunos, Disciplinas, Turmas, Séries, Períodos nem Usuários — essas telas ficam " +
                        "escondidas de propósito, porque não fazem parte do trabalho dele.");
                    Print(column, imagens["dashboard-professor"], "Painel Inicial visto por um Professor: repare no menu lateral bem mais curto.");
                    Paragrafo(column,
                        "Além do menu mais curto, dentro de Notas e Resultados o Professor só enxerga os próprios alunos: exatamente os alunos " +
                        "das turmas e disciplinas que estão vinculadas a ele na tela de Professores. Ele nunca vê notas ou resultados de turmas " +
                        "de outro professor.");
                    Print(column, imagens["notas-professor"], "Notas vista por um Professor: aparecem só os lançamentos dele mesmo.");
                    Print(column, imagens["resultados-professor"], "Resultados vista por um Professor: aparecem só os alunos dele mesmo.");

                    column.Item().PageBreak();
                    Titulo(column, "16. Resumo: o que cada perfil pode fazer");
                    Paragrafo(column, "Uma tabela rápida para consultar sempre que tiver dúvida sobre alguma permissão:");
                    TabelaPermissoes(column);

                    column.Item().PageBreak();
                    Titulo(column, "17. Problemas comuns e como resolver");
                    Bullet(column, "\"Não consigo lançar nota\": confira se já existe um Período cadastrado para aquele ano/trimestre, e se ele está Aberto.");
                    Bullet(column, "\"O botão de editar a nota sumiu\": o período dela provavelmente está Fechado — só o Diretor pode reabrir o período ou editar a nota.");
                    Bullet(column, "\"CPF ou senha inválidos\": confira se digitou o CPF certo e a senha corretamente (maiúsculas/minúsculas importam). Se persistir, peça para o Diretor redefinir sua senha.");
                    Bullet(column, "\"Esqueci a senha\": qualquer Diretor, Coordenador ou Secretária pode redefinir a sua senha na tela de Usuários (ou de Professores, se você for professor).");
                    Bullet(column, "\"Aluno aparece como Pendente nos Resultados\": significa que falta lançar alguma nota dele em algum trimestre daquele ano letivo.");

                    Titulo(column, "18. Controle de versão");
                    Paragrafo(column, $"Este manual acompanha a versão {appVersion} do Sistema Escolar, desenvolvido por Well Tech.");
                });

                page.Footer()
                    .AlignRight()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        var bytes = content.GeneratePdf();
        return File(bytes, "application/pdf", $"manual-utilizacao-sistema-escolar-v{appVersion}.pdf");
    }

    private static void Titulo(ColumnDescriptor column, string texto) =>
        column.Item().PaddingTop(14).Text(texto).Bold().FontSize(14).FontColor(Colors.Blue.Darken1);

    private static void Paragrafo(ColumnDescriptor column, string texto) =>
        column.Item().Text(texto).FontSize(10.5f).LineHeight(1.35f);

    private static void Bullet(ColumnDescriptor column, string texto)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(14).Text("•").FontSize(10.5f);
            row.RelativeItem().Text(texto).FontSize(10.5f).LineHeight(1.3f);
        });
    }

    private static void Print(ColumnDescriptor column, byte[] imagem, string legenda)
    {
        column.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Image(imagem).FitWidth();
        column.Item().AlignCenter().Text(legenda).FontSize(8.5f).FontColor(Colors.Grey.Darken1).Italic();
    }

    private static void TabelaPermissoes(ColumnDescriptor column)
    {
        column.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
            });

            void CabecalhoCelula(string texto) => table.Cell().Background(Colors.Blue.Darken1)
                .Padding(4).Text(texto).FontColor(Colors.White).SemiBold().FontSize(8.5f);

            CabecalhoCelula("Tela / Ação");
            CabecalhoCelula("Diretor");
            CabecalhoCelula("Coordenador");
            CabecalhoCelula("Secretária");
            CabecalhoCelula("Professor");

            void Celula(string texto, bool destaqueNegativo = false)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(texto).FontSize(8.5f).FontColor(destaqueNegativo ? Colors.Red.Darken1 : Colors.Black);
            }

            void Linha(string acao, string diretor, string coordenador, string secretaria, string professor)
            {
                Celula(acao);
                Celula(diretor, diretor is "Não");
                Celula(coordenador, coordenador is "Não");
                Celula(secretaria, secretaria is "Não");
                Celula(professor, professor is "Não");
            }

            Linha("Ver o Painel Inicial", "Sim", "Sim", "Sim", "Sim (versão simplificada)");
            Linha("Cadastrar/editar Alunos", "Sim", "Sim", "Sim", "Não");
            Linha("Cadastrar/editar Disciplinas", "Sim", "Sim", "Sim", "Não");
            Linha("Cadastrar/editar Professores", "Sim", "Sim", "Sim", "Não");
            Linha("Cadastrar/editar Turmas e Séries", "Sim", "Sim", "Sim", "Não");
            Linha("Ver Períodos de Lançamento", "Sim", "Sim", "Sim", "Não");
            Linha("Abrir/Fechar um Período", "Sim", "Não", "Não", "Não");
            Linha("Lançar/ver Notas", "Sim (todas)", "Sim (todas)", "Sim (todas)", "Só as suas turmas");
            Linha("Usar o Lançamento de Notas em Massa", "Sim (todas)", "Sim (todas)", "Sim (todas)", "Só as suas turmas");
            Linha("Editar/excluir Nota em período Fechado", "Sim", "Não", "Não", "Não");
            Linha("Ver/exportar Resultados", "Sim (todos)", "Sim (todos)", "Sim (todos)", "Só os seus alunos");
            Linha("Lançar a Recuperação Final", "Sim", "Sim", "Sim", "Só os seus alunos");
            Linha("Baixar o Boletim do Aluno em PDF", "Sim", "Sim", "Sim", "Não");
            Linha("Gerenciar Usuários (Diretor/Coord./Secretária)", "Sim", "Sim", "Sim", "Não");
            Linha("Baixar este Manual", "Sim", "Sim", "Sim", "Sim");
        });
    }

    private Dictionary<string, byte[]> CarregarImagens()
    {
        var pasta = Path.Combine(_environment.WebRootPath, "manual-assets");
        var nomes = new[]
        {
            "login", "dashboard-admin", "alunos", "disciplinas", "professores", "turmas", "series",
            "periodos-diretor", "periodos-coordenador", "notas-diretor", "boletim-aluno", "notas-lancamento-massa",
            "resultados-diretor", "usuarios", "trocar-senha", "dashboard-professor", "notas-professor", "resultados-professor",
        };

        return nomes.ToDictionary(nome => nome, nome => System.IO.File.ReadAllBytes(Path.Combine(pasta, $"{nome}.png")));
    }
}
