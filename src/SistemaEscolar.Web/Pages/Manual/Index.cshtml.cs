using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaEscolar.Web.Pages.Manual;

public sealed class IndexModel : PageModel
{
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

        var content = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("Manual de Utilização - Sistema Escolar")
                    .SemiBold()
                    .FontSize(16)
                    .FontColor(Colors.Blue.Medium);

                page.Content().Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Text($"Versão da aplicação: {appVersion}");
                    column.Item().Text($"Data de emissão: {DateTime.Now:dd/MM/yyyy}");

                    column.Item().Text("1. Objetivo").SemiBold();
                    column.Item().Text("Orientar o uso do Sistema Escolar para os perfis Diretor, Coordenador e Professor.");

                    column.Item().Text("2. Acesso").SemiBold();
                    column.Item().Text("Acesse o sistema, informe usuário e senha e clique em Entrar.");

                    column.Item().Text("3. Módulos").SemiBold();
                    column.Item().Text("Dashboard, Alunos, Disciplinas, Professores, Turmas, Séries, Períodos, Notas e Resultados.");

                    column.Item().Text("4. Fluxo recomendado").SemiBold();
                    column.Item().Text("Cadastre séries e turmas, depois disciplinas e professores, alunos, períodos e por fim lance notas.");

                    column.Item().Text("5. Notas e cálculos").SemiBold();
                    column.Item().Text("Cada trimestre possui Avaliação 1, 2 e 3, com Recuperação Paralela opcional.");
                    column.Item().Text("Resultado da Unidade = média das 3 avaliações.");
                    column.Item().Text("Resultado Final da Unidade = maior valor entre Resultado da Unidade e Recuperação Paralela.");

                    column.Item().Text("6. Regras de negócio").SemiBold();
                    column.Item().Text("Trimestres válidos: 1 a 3. Faixa de notas: 0 a 10.");
                    column.Item().Text("Média mínima para aprovação: 5,0.");
                    column.Item().Text("Sem todos os lançamentos do ano, a situação do aluno fica como Pendente.");

                    column.Item().Text("7. Exportações").SemiBold();
                    column.Item().Text("Na tela de Resultados, é possível exportar CSV, XLSX e PDF.");

                    column.Item().Text("8. Problemas comuns").SemiBold();
                    column.Item().Text("Sem período configurado: cadastre o período do ano/trimestre correspondente.");
                    column.Item().Text("Período fechado: apenas Diretor pode alterar/excluir notas.");
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
}
