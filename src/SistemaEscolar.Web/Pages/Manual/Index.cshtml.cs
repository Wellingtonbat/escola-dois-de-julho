using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Abstractions;

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

                page.Content().Column(column =>
                {
                    column.Spacing(6);

                    // Capa: o título aparece só aqui, na primeira página.
                    column.Item().PaddingBottom(6).Text("Manual de Utilização - Escola Dois de Julho")
                        .SemiBold().FontSize(15).FontColor(Colors.Blue.Darken1);
                    column.Item().Text($"Versão da aplicação: {appVersion}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    column.Item().Text($"Data de emissão: {HorarioBrasilia.Agora:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Darken1);

                    Paragrafo(column,
                        "Este manual explica, de um jeito bem simples, como usar o sistema da Escola Dois de Julho. " +
                        "Não se preocupe se você nunca usou o sistema antes: vamos explicar cada tela como se " +
                        "estivéssemos mostrando pessoalmente, com direito a fotos de cada parte!");

                    Titulo(column, "1. O que é o sistema da Escola Dois de Julho?");
                    Paragrafo(column,
                        "Pense no sistema da Escola Dois de Julho como um caderno gigante e organizado da escola, só que dentro do computador. " +
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
                        "pessoa pode abrir. Existem 5 crachás diferentes:");
                    Bullet(column, "Diretor(a) — o crachá mais completo. Pode fazer absolutamente tudo no sistema, inclusive coisas que mais ninguém pode, como alterar notas com o trimestre fechado e excluir um período.");
                    Bullet(column, "Vice-Diretor(a) — tem exatamente as mesmas permissões do Diretor. Na escola, costuma ser um professor: ele continua com as turmas e disciplinas dele, como qualquer professor, mas enxerga e faz tudo o que o Diretor faz. Quem marca um professor como Vice-Diretor é o Diretor (ou outro Vice-Diretor), na tela de Professores.");
                    Bullet(column, "Coordenador(a) — cuida do dia a dia acadêmico (alunos, turmas, professores) e pode abrir e fechar trimestres. As notas são só de consulta: o Coordenador vê tudo, mas não altera nenhuma nota.");
                    Bullet(column, "Secretária — cuida dos cadastros e consulta as notas, mas não altera nenhuma nota e também não abre nem fecha trimestres.");
                    Bullet(column, "Professor(a) — o crachá mais simples. Só enxerga as suas próprias turmas e disciplinas, e só pode lançar as notas dos alunos que ele mesmo dá aula, enquanto o trimestre estiver aberto.");
                    Paragrafo(column,
                        "Guarde essa ideia: Diretor, Vice-Diretor, Coordenador e Secretária enxergam o sistema quase do mesmo jeito (menu completo). " +
                        "Já o Professor enxerga um sistema bem mais enxuto, com menos botões — isso é de propósito, para simplificar o dia a dia dele. " +
                        "Sobre notas: quem altera são o Diretor, o Vice-Diretor (em qualquer período) e o Professor (só com o trimestre aberto, nas suas turmas). " +
                        "Coordenador e Secretária apenas consultam.");

                    Titulo(column, "4. O Painel Inicial (Dashboard)");
                    Paragrafo(column,
                        "É a primeira tela que aparece depois que você entra. Pense nela como o painel de um carro: num olhar só, você " +
                        "fica sabendo como a escola está indo — quantos alunos estão indo bem, quantos precisam de atenção e quantas notas " +
                        "ainda faltam lançar. Diretor, Vice-Diretor, Coordenador e Secretária veem o painel completo; o Professor vê os " +
                        "mesmos gráficos, com uma diferença que explico no final desta seção.");
                    Print(column, imagens["dashboard-admin"], "Visão geral do Painel Inicial (Diretor, Vice-Diretor, Coordenador ou Secretária).");

                    Paragrafo(column,
                        "Começando pelos filtros, lá em cima: Professor, Turma, Disciplina, Ano letivo e Trimestre. Funciona como uma " +
                        "lupa. Se você não escolher nada, vê a escola inteira no ano todo. Quer ver só o 7º Ano A no 1º trimestre? " +
                        "Escolha a turma, escolha o trimestre e clique em \"Filtrar\" — os números e os gráficos se atualizam na hora, " +
                        "sem recarregar a página. Logo acima dos cartões aparece uma frase lembrando o que está filtrado (por exemplo, " +
                        "\"7º Ano A · Todas as disciplinas · 1º Trimestre/2026\"). O botão \"Limpar\" volta tudo para a escola inteira. " +
                        "Detalhe: o campo Turma só mostra as turmas do ano letivo escolhido.");
                    Print(column, imagens["dash-filtros"], "Os filtros do Painel Inicial: escolha o que quer ver e clique em \"Filtrar\".");

                    Paragrafo(column, "Agora, os quatro cartões coloridos do topo. Eles sempre acompanham os filtros:");
                    Bullet(column,
                        "Alunos matriculados — quantos alunos ativos existem no recorte escolhido. Aluno que foi transferido ou saiu da " +
                        "escola (inativo) não entra nessa conta, nem em nenhum outro número do painel.");
                    Bullet(column,
                        "Aprovação geral — de todas as notas de trimestre que já foram lançadas, quantas ficaram com 5,0 ou mais. Repare " +
                        "que só conta o que já foi lançado: se a escola lançou 100 notas e 80 ficaram acima da média, aparece 80%, mesmo " +
                        "que ainda faltem outras notas para lançar.");
                    Bullet(column,
                        "Pendências de lançamento — quantas notas ainda faltam. A conta é simples: o sistema sabe quantas notas deveriam " +
                        "existir (cada aluno, em cada disciplina da turma dele, em cada trimestre) e tira as que já foram lançadas. " +
                        "Uma turma de 30 alunos com 10 disciplinas, no ano todo, deveria ter 30 × 10 × 3 = 900 notas; se 600 já foram " +
                        "lançadas, faltam 300.");
                    Bullet(column,
                        "Períodos em aberto — quantos trimestres estão liberados para lançar notas hoje, de quantos existem no ano (ex.: " +
                        "\"1 / 3\"). Conta o trimestre que está dentro das datas dele ou que foi aberto manualmente pela Direção.");
                    Print(column, imagens["dash-cartoes"], "Os quatro cartões do topo: alunos, aprovação, notas que faltam e trimestres abertos.");

                    Paragrafo(column, "Descendo um pouco, vêm os gráficos:");
                    Bullet(column,
                        "Média por disciplina — é preciso escolher uma turma para ele aparecer. Cada barra é uma matéria, com a média " +
                        "das notas daquela turma; a linha tracejada marca o 5,0, então dá para ver na hora quem está abaixo. Se uma " +
                        "matéria ainda não tem nenhuma nota no período, ela aparece com um tracinho (\"—\") em vez de sumir.");
                    PrintMenor(column, imagens["dash-media-disciplina"], "Média por disciplina de uma turma, com a linha tracejada do 5,0.");
                    Bullet(column,
                        "Aprovados x Reprovados x Pendentes — o gráfico redondo (de rosca). Ele divide todas as notas esperadas em três " +
                        "fatias: as lançadas com 5,0 ou mais, as lançadas abaixo de 5,0 e as que ainda faltam lançar. O número grande " +
                        "no meio é o mesmo da \"Aprovação geral\" do cartão lá de cima.");
                    PrintMenor(column, imagens["dash-rosca"], "Gráfico de rosca: aprovados, reprovados e notas que ainda faltam lançar.");
                    Bullet(column,
                        "Evolução por trimestre — uma linha que mostra a média do 1º, 2º e 3º trimestres, para você ver se a turma (ou a " +
                        "escola) está melhorando ou piorando ao longo do ano. Por isso ele mostra sempre os três trimestres, mesmo que você " +
                        "tenha escolhido um trimestre no filtro.");
                    PrintMenor(column, imagens["dash-evolucao"], "Evolução da média ao longo dos trimestres.");
                    Bullet(column,
                        "Turmas por índice de aprovação — um ranking: de um lado, as 5 turmas com mais notas acima da média; do outro, as " +
                        "5 que mais precisam de atenção. Se a escola tiver poucas turmas, elas são divididas entre os dois lados, sem " +
                        "repetir nenhuma. Como a ideia é comparar as turmas entre si, este quadro mostra todas as turmas, mesmo com uma " +
                        "turma escolhida no filtro.");
                    PrintMenor(column, imagens["dash-ranking"], "Ranking: turmas com melhor desempenho e turmas que precisam de atenção.");
                    Bullet(column,
                        "Mapa de pendências — uma tabela que cruza turmas (nas linhas) com disciplinas (nas colunas). Cada quadradinho " +
                        "mostra quantas notas já foram lançadas de quantas deveriam existir, e a cor ajuda a achar o problema rápido: " +
                        "verde = tudo lançado; amarelo = falta pouco (até 30%); vermelho = falta muito (mais de 30%). Assim como o " +
                        "ranking, ele mostra todas as turmas, para você enxergar a escola inteira de uma vez.");
                    Print(column, imagens["dash-mapa"], "Mapa de pendências: cada quadradinho mostra notas lançadas / notas esperadas.");

                    Paragrafo(column,
                        "E o Professor? Ele vê esses mesmos gráficos, mas no lugar do filtro de Professor aparece o campo \"Visão\", com " +
                        "duas opções: \"Minhas turmas\" (o padrão), que mostra só as turmas e disciplinas dele, e \"Escola inteira\", " +
                        "para ele comparar o desempenho das suas turmas com o da escola. Ele não consegue escolher outro professor. Mais " +
                        "detalhes na seção 15.");
                    Print(column, imagens["dash-visao-professor"], "Filtros vistos pelo Professor: no lugar de \"Professor\" aparece o campo \"Visão\".");
                    Paragrafo(column,
                        "Uma dica final: se algum número parecer estranho, confira primeiro os filtros (a frase acima dos cartões mostra " +
                        "o que está selecionado) e se as notas daquele período já foram todas lançadas.");

                    column.Item().PageBreak();
                    Titulo(column, "5. Alunos");
                    Paragrafo(column,
                        "Aqui ficam cadastrados todos os alunos da escola: nome, CPF, data de nascimento, série e turma. " +
                        "Diretor, Vice-Diretor, Coordenador e Secretária podem cadastrar, editar, ativar/desativar e excluir alunos. " +
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
                    Paragrafo(column,
                        "Quando o professor também é Vice-Diretor, o Diretor (ou outro Vice-Diretor) marca a opção \"Também é Vice-Diretor\" " +
                        "no cadastro dele. As turmas e disciplinas dele continuam valendo normalmente, mas ele passa a ter as permissões do Diretor: " +
                        "vê o menu completo e as notas e resultados de toda a escola. Essa opção só aparece para Diretor e Vice-Diretor, e a conta " +
                        "de um Vice-Diretor (senha, exclusão) também só é gerenciada por eles. O novo perfil vale a partir do próximo login da pessoa.");
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
                        "ou Fechado (as notas ficam travadas). Por padrão, isso acontece sozinho: o período fica Aberto automaticamente entre " +
                        "a Data Inicial e a Data Final cadastradas, e fecha sozinho quando a Data Final passa.");
                    Paragrafo(column,
                        "Precisa lançar uma nota de um trimestre que já encerrou? O botão \"Abrir\" força uma exceção manual: o período fica " +
                        "aberto mesmo depois da Data Final, até que alguém feche ele de novo — o fechamento deixa de ser automático nesse caso, " +
                        "então é preciso lembrar de fechar quando terminar. Para ajudar a não esquecer, o Painel Inicial mostra um aviso em " +
                        "vermelho para Diretor, Vice-Diretor, Coordenador e Secretária sempre que existir um período aberto assim, além da data.");
                    Paragrafo(column,
                        "Aqui está a primeira grande diferença entre os perfis: Diretor, Vice-Diretor e Coordenador têm os botões \"Abrir\" e \"Fechar\" " +
                        "período (mostrados na foto abaixo). A Secretária consegue cadastrar e editar períodos, mas não consegue trocar o " +
                        "status de aberto/fechado — esses botões não aparecem para ela (ela só vê o aviso no Painel Inicial, caso exista).");
                    Print(column, imagens["periodos-diretor"], "Períodos visto pelo Diretor, Vice-Diretor ou Coordenador: repare nos botões \"Abrir\"/\"Fechar\".");

                    column.Item().PageBreak();
                    Titulo(column, "10. Notas");
                    Paragrafo(column,
                        "É onde as notas de cada aluno, em cada disciplina e período, são lançadas. Para cada lançamento existem até 4 números:");
                    Bullet(column, "Avaliação 1, Avaliação 2 e Avaliação 3 — as três provas/atividades do trimestre (de 0 a 10).");
                    Bullet(column, "Recuperação Paralela — uma nota extra, para quem não alcançou 5,0 no trimestre.");
                    Paragrafo(column, "O sistema faz as contas sozinho, seguindo esta receita:");
                    Bullet(column, "Resultado da Unidade = a soma das 3 avaliações (por exemplo, 3,0 + 2,5 + 3,5 = 9,0). A soma não pode passar de 10,0.");
                    Bullet(column, "Recuperação Paralela = só fica liberada quando essa soma dá menos de 5,0.");
                    Bullet(column, "Resultado Final da Unidade = o maior valor entre o Resultado da Unidade e a Recuperação Paralela (quando ela existir).");
                    Paragrafo(column,
                        "Importante: se o período estiver Fechado, somente o Diretor ou o Vice-Diretor conseguem editar ou excluir uma nota já lançada nele. " +
                        "O Professor só altera notas com o período Aberto, e apenas as das suas turmas e disciplinas.");
                    Paragrafo(column,
                        "Coordenador e Secretária têm acesso somente de consulta às notas: eles veem a lista, abrem o boletim e baixam os PDFs, " +
                        "mas os botões de lançar, finalizar e excluir não aparecem para eles (e o sistema também recusa a alteração, " +
                        "mesmo que alguém tente por outro caminho).");
                    Print(column, imagens["notas-diretor"], "Tela de Notas vista pelo Diretor: mostra os lançamentos de todos os professores.");

                    column.Item().PageBreak();
                    Titulo(column, "11. Boletim do Aluno e Lançamento de Notas em Massa");
                    Paragrafo(column,
                        "Clicando em qualquer lançamento da lista de Notas, você abre o Boletim completo daquele aluno: todas as " +
                        "disciplinas do ano, lado a lado, com a situação (Aprovado, Reprovado ou Em andamento) em cada uma. É a forma " +
                        "mais rápida de ver — ou lançar — todas as notas de um único aluno de uma vez só.");
                    Print(column, imagens["boletim-aluno"], "Boletim do Aluno: todas as disciplinas do ano, com a situação em cada uma.");
                    Paragrafo(column,
                        "Se você é Diretor, Vice-Diretor, Coordenador ou Secretária, o botão \"Baixar Boletim (PDF)\" gera esse boletim no mesmo modelo " +
                        "impresso usado pela escola — pronto para imprimir ou anexar em processos.");
                    Paragrafo(column,
                        "E quando é preciso entregar o boletim de uma turma inteira de uma vez — no fechamento do trimestre, por exemplo — " +
                        "o botão \"Baixar Boletins da Turma\", no topo da tela de Notas, gera um único PDF com o boletim de todos os alunos " +
                        "ativos daquela turma, um por página, pronto para imprimir e distribuir. Esse botão também é exclusivo de Diretor, " +
                        "Vice-Diretor, Coordenador e Secretária.");
                    Paragrafo(column,
                        "E quando é preciso lançar a nota de uma turma inteira, aluno por aluno, um de cada vez fica trabalhoso. Por isso " +
                        "existe o botão \"Lançamento em Massa\", no topo da tela de Notas: você escolhe o trimestre, a turma e a disciplina " +
                        "uma única vez, e o sistema abre uma planilha com todos os alunos daquela turma, um em cada linha.");
                    Bullet(column, "Digite as notas e aperte Tab para pular de campo em campo — cada nota é salva sozinha assim que você sai do campo, sem precisar clicar em nenhum botão de salvar.");
                    Bullet(column, "A coluna \"Status\" muda de cor na hora, mostrando se aquele aluno já está Aprovado, Reprovado ou ainda Não lançado.");
                    Bullet(column, "O Professor só enxerga, também aqui, as turmas e disciplinas que ele mesmo leciona — a mesma regra de sempre.");
                    Print(column, imagens["notas-lancamento-massa"], "Lançamento em Massa: uma planilha com todos os alunos da turma, notas salvando sozinhas.");

                    column.Item().PageBreak();
                    Titulo(column, "12. Resultados");
                    Paragrafo(column,
                        "É o boletim final. Junta as notas de todas as disciplinas de um aluno no ano e mostra a situação dele:");
                    Bullet(column, "Aprovado — média final igual ou maior que 5,0, com todas as notas do ano lançadas.");
                    Bullet(column, "Reprovado — média final menor que 5,0, mesmo depois de considerar a Avaliação Final (quando existir).");
                    Bullet(column, "Pendente — ainda falta lançar alguma nota do ano; o sistema ainda não consegue calcular o resultado final.");
                    Paragrafo(column,
                        "E se o aluno terminar o ano abaixo da média? Aí entra a Avaliação Final — a última chance dele recuperar a " +
                        "disciplina no ano. Ela é lançada aqui mesmo, na tela de Resultados, e só pelo Diretor ou pelo Vice-Diretor. " +
                        "Os outros perfis conseguem ver a nota, mas não lançam.");
                    Paragrafo(column,
                        "Quando ela pode ser lançada? Só depois que os 3 trimestres daquela disciplina já foram lançados e, mesmo assim, " +
                        "a média do ano ficou abaixo de 5,0. Enquanto isso não acontece, o campinho nem aparece — e é isso mesmo, porque " +
                        "ainda não dá para saber se o aluno vai precisar dela.");
                    Paragrafo(column, "Passo a passo para lançar:");
                    Bullet(column,
                        "1º — Entre com o seu usuário de Diretor ou Vice-Diretor e clique em \"Resultados\" no menu lateral.");
                    Bullet(column,
                        "2º — Nos filtros, escolha o Ano letivo, a Turma e, em Situação, clique em \"Reprovados\". Clique em \"Filtrar\". " +
                        "Assim a lista mostra só quem está precisando da Avaliação Final, sem você ter que procurar aluno por aluno.");
                    Print(column, imagens["avf-filtro"], "Filtro de Resultados com a turma escolhida e a situação \"Reprovados\".");
                    Bullet(column,
                        "3º — Na coluna \"Avaliação Final\", ache o aluno e digite a nota no campinho (de 0 a 10; pode usar vírgula, " +
                        "como 6,5). Depois clique em \"Salvar\" e confirme na janelinha que aparece.");
                    Print(column, imagens["avf-tabela"], "O campinho da Avaliação Final: o primeiro aluno ainda está esperando a nota; o segundo já tem a dele lançada.");
                    Bullet(column,
                        "4º — Pronto! O Resultado Final e a Situação do aluno se atualizam na hora.");
                    Paragrafo(column,
                        "Como o sistema usa essa nota? Vale sempre a maior entre a média do ano e a Avaliação Final. Um exemplo: o aluno " +
                        "terminou Matemática com média 3,0. Se tirar 6,0 na Avaliação Final, o resultado dele vira 6,0 e ele fica " +
                        "Aprovado. Se tirar 2,0, continua valendo o 3,0 (a nota maior) e ele fica Reprovado na disciplina — e, na Ata, " +
                        "aparece como Conservado(a).");
                    Paragrafo(column, "Algumas situações que costumam gerar dúvida:");
                    Bullet(column,
                        "Lancei a nota errada — é só digitar a nota certa no mesmo campinho e clicar em \"Salvar\" de novo. A nova nota " +
                        "substitui a anterior (e a troca fica registrada na Auditoria).");
                    Bullet(column,
                        "O aluno não fez a prova — enquanto não houver nota, ele fica \"Pendente\" na Ata e a Ata não pode ser finalizada. " +
                        "Se ele faltou de vez, lance 0: aí vale a média dele, e a Ata mostra o resultado.");
                    Bullet(column,
                        "O campinho não aparece — confira se os 3 trimestres daquela disciplina já foram lançados, se a média do ano " +
                        "está mesmo abaixo de 5,0 e se você entrou com um usuário de Diretor ou Vice-Diretor.");
                    Paragrafo(column,
                        "A Ata de Resultados Finais (menu Atas) usa exatamente esses resultados. Enquanto está em Rascunho, ela acompanha " +
                        "as notas e a Avaliação Final em tempo real e calcula o resultado de cada aluno: Aprovado(a) quando todas as " +
                        "disciplinas ficam com 5,0 ou mais; Conservado(a) quando alguma fica abaixo de 5,0 mesmo depois da Avaliação Final; " +
                        "e Pendente enquanto falta alguma nota ou alguma Avaliação Final. Alunos inativos (quem saiu da escola) entram como " +
                        "Transferido(a), e só para eles dá para trocar para Deixou de frequentar. A coluna Apto a Cursar é preenchida pela " +
                        "série. Ao finalizar, a Ata é \"congelada\" com as notas daquele momento.");
                    Paragrafo(column,
                        "Nesta tela também dá para filtrar por turma, série e situação, buscar por aluno, disciplina ou professor, e " +
                        "exportar a lista em CSV, Excel (XLSX) ou PDF. Na exportação em Excel, cada disciplina ganha sua própria aba, " +
                        "já no mesmo formato de planilha usado pela escola.");
                    Print(column, imagens["resultados-diretor"], "Tela de Resultados vista pelo Diretor: todos os alunos da escola.");

                    column.Item().PageBreak();
                    Titulo(column, "13. Usuários (só para Diretor, Vice-Diretor, Coordenador e Secretária)");
                    Paragrafo(column,
                        "Essa tela serve para criar os logins de Diretor, Vice-Diretor, Coordenador e Secretária (os logins de Professor são criados " +
                        "automaticamente lá na tela de Professores). Quem cria um usuário novo escolhe uma senha padrão, e o sistema obriga " +
                        "a pessoa a trocar essa senha assim que ela entrar pela primeira vez — é uma proteção extra.");
                    Paragrafo(column,
                        "Por segurança, as contas de Diretor e de Vice-Diretor só podem ser criadas, editadas, ativadas/desativadas, excluídas " +
                        "ou ter a senha redefinida por outro Diretor ou Vice-Diretor. Para Coordenador e Secretária essas opções aparecem para " +
                        "todos os perfis administrativos.");
                    Print(column, imagens["usuarios"], "Tela de Usuários: cadastro de Diretor, Vice-Diretor, Coordenador e Secretária.");
                    Print(column, imagens["trocar-senha"], "Tela mostrada automaticamente no primeiro acesso, obrigando a criar uma nova senha.");

                    Titulo(column, "14. Auditoria (só para Diretor e Vice-Diretor)");
                    Paragrafo(column,
                        "A tela de Auditoria responde a pergunta \"quem mexeu nisso e quando?\". Toda vez que alguém cadastra, altera ou exclui " +
                        "algo no sistema (uma nota, um aluno, um período, um usuário, o perfil de alguém...), fica um registro com o dia e a hora " +
                        "(horário de Brasília), o nome e o CPF de quem fez, o que foi mexido e, nas alterações, o valor de antes e o de depois.");
                    Bullet(column, "Filtros — período (por padrão, os últimos 30 dias), usuário (por nome ou CPF), o que foi alterado (notas, alunos, professores, usuários...) e o tipo de ação (criado, alterado ou excluído).");
                    Bullet(column, "Detalhes — clique em \"campo(s)\" na última coluna para ver, campo a campo, o que mudou. Por exemplo: 1ª avaliação: 5,5 → 7,25.");
                    Bullet(column, "Privacidade — senhas nunca aparecem: uma troca de senha é registrada apenas como \"Senha: alterada\". Logins e tentativas de login não entram na lista.");
                    Bullet(column, "Acesso — somente Diretor e Vice-Diretor veem esta tela; para os demais perfis o menu \"Auditoria\" nem aparece. Registros anteriores a esta função podem aparecer como \"Sistema / não identificado\", pois na época o sistema ainda não guardava quem fez a ação.");

                    column.Item().PageBreak();
                    Titulo(column, "15. O que o Professor enxerga");
                    Paragrafo(column,
                        "Quando um Professor entra no sistema, o menu lateral aparece bem mais curto: só Dashboard, Notas, Resultados e o botão " +
                        "para baixar este manual. Ele não vê Alunos, Disciplinas, Turmas, Séries, Períodos nem Usuários — essas telas ficam " +
                        "escondidas de propósito, porque não fazem parte do trabalho dele.");
                    Print(column, imagens["dashboard-professor"], "Painel Inicial visto por um Professor: repare no menu lateral bem mais curto.");
                    Paragrafo(column,
                        "No Painel Inicial, o Professor vê os mesmos gráficos da Direção. No lugar do filtro de Professor existe o campo " +
                        "\"Visão\": \"Minhas turmas\" (o padrão) mostra só as turmas e disciplinas vinculadas a ele, e \"Escola inteira\" " +
                        "mostra os números gerais da escola, para comparação. Ele não consegue escolher outro professor.");
                    Paragrafo(column,
                        "Além do menu mais curto, dentro de Notas e Resultados o Professor só enxerga os próprios alunos: exatamente os alunos " +
                        "das turmas e disciplinas que estão vinculadas a ele na tela de Professores. Ele nunca vê notas ou resultados de turmas " +
                        "de outro professor.");
                    Print(column, imagens["notas-professor"], "Notas vista por um Professor: aparecem só os lançamentos dele mesmo.");
                    Print(column, imagens["resultados-professor"], "Resultados vista por um Professor: aparecem só os alunos dele mesmo.");
                    Paragrafo(column,
                        "Atenção: essa visão simplificada vale só para quem é apenas Professor. Um professor que também é Vice-Diretor " +
                        "vê o sistema completo, como o Diretor.");

                    column.Item().PageBreak();
                    Titulo(column, "16. Resumo: o que cada perfil pode fazer");
                    Paragrafo(column, "Uma tabela rápida para consultar sempre que tiver dúvida sobre alguma permissão:");
                    TabelaPermissoes(column);

                    column.Item().PageBreak();
                    Titulo(column, "17. Problemas comuns e como resolver");
                    Bullet(column, "\"Não consigo lançar nota\": confira se já existe um Período cadastrado para aquele ano/trimestre, e se ele está Aberto.");
                    Bullet(column, "\"O botão de editar a nota sumiu\": o período dela provavelmente está Fechado — só o Diretor ou o Vice-Diretor podem editar a nota (ou o Diretor, Vice-Diretor ou Coordenador podem reabrir o período). Se o seu perfil é Coordenador ou Secretária, as notas são somente de consulta.");
                    Bullet(column, "\"CPF ou senha inválidos\": confira se digitou o CPF certo e a senha corretamente (maiúsculas/minúsculas importam). Se persistir, peça para o Diretor redefinir sua senha.");
                    Bullet(column, "\"Esqueci a senha\": qualquer Diretor, Vice-Diretor, Coordenador ou Secretária pode redefinir a sua senha na tela de Usuários (ou de Professores, se você for professor). Contas de Diretor e Vice-Diretor só podem ser redefinidas por outro Diretor ou Vice-Diretor.");
                    Bullet(column, "\"Aluno aparece como Pendente nos Resultados\": significa que falta lançar alguma nota dele em algum trimestre daquele ano letivo.");

                    Titulo(column, "18. Controle de versão");
                    Paragrafo(column, $"Este manual acompanha a versão {appVersion} do sistema da Escola Dois de Julho, desenvolvido por Well Tech.");
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

    // Para recortes de um gráfico só (metade da tela): mais estreito e centralizado, para não ficar gigante.
    private static void PrintMenor(ColumnDescriptor column, byte[] imagem, string legenda)
    {
        column.Item().PaddingTop(4).AlignCenter().Width(330).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Image(imagem).FitWidth();
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
            CabecalhoCelula("Diretor / Vice-Diretor");
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

            Linha("Ver o Painel Inicial", "Sim", "Sim", "Sim", "Sim (suas turmas ou escola inteira)");
            Linha("Cadastrar/editar Alunos", "Sim", "Sim", "Sim", "Não");
            Linha("Cadastrar/editar Disciplinas", "Sim", "Sim", "Sim", "Não");
            Linha("Cadastrar/editar Professores", "Sim", "Sim", "Sim", "Não");
            Linha("Cadastrar/editar Turmas e Séries", "Sim", "Sim", "Sim", "Não");
            Linha("Ver Períodos de Lançamento", "Sim", "Sim", "Sim", "Não");
            Linha("Abrir/Fechar um Período", "Sim", "Sim", "Não", "Não");
            Linha("Excluir um Período", "Sim", "Não", "Não", "Não");
            Linha("Ver Notas (consulta)", "Sim (todas)", "Sim (todas)", "Sim (todas)", "Só as suas turmas");
            Linha("Lançar/editar/excluir Notas (período Aberto)", "Sim (todas)", "Não", "Não", "Só as suas turmas");
            Linha("Usar o Lançamento de Notas em Massa", "Sim (todas)", "Não", "Não", "Só as suas turmas");
            Linha("Editar/excluir Nota em período Fechado", "Sim", "Não", "Não", "Não");
            Linha("Ver/exportar Resultados", "Sim (todos)", "Sim (todos)", "Sim (todos)", "Só os seus alunos");
            Linha("Lançar a Avaliação Final", "Sim", "Não", "Não", "Não");
            Linha("Baixar o Boletim do Aluno em PDF", "Sim", "Sim", "Sim", "Não");
            Linha("Baixar os Boletins de uma Turma em PDF", "Sim", "Sim", "Sim", "Não");
            Linha("Gerenciar Usuários (Coord./Secretária)", "Sim", "Sim", "Sim", "Não");
            Linha("Gerenciar contas de Diretor e Vice-Diretor", "Sim", "Não", "Não", "Não");
            Linha("Marcar um Professor como Vice-Diretor", "Sim", "Não", "Não", "Não");
            Linha("Consultar a Auditoria", "Sim", "Não", "Não", "Não");
            Linha("Baixar este Manual", "Sim", "Sim", "Sim", "Sim");
        });
    }

    private Dictionary<string, byte[]> CarregarImagens()
    {
        var pasta = Path.Combine(_environment.WebRootPath, "manual-assets");
        var nomes = new[]
        {
            "login", "dashboard-admin", "alunos", "disciplinas", "professores", "turmas", "series",
            "periodos-diretor", "notas-diretor", "boletim-aluno", "notas-lancamento-massa",
            "resultados-diretor", "usuarios", "trocar-senha", "dashboard-professor", "notas-professor", "resultados-professor",
            "dash-filtros", "dash-cartoes", "dash-media-disciplina", "dash-rosca", "dash-evolucao", "dash-ranking", "dash-mapa",
            "dash-visao-professor", "avf-filtro", "avf-tabela",
        };

        return nomes.ToDictionary(nome => nome, nome => System.IO.File.ReadAllBytes(Path.Combine(pasta, $"{nome}.png")));
    }
}
