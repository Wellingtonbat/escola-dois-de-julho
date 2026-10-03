using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Atas;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages.Atas;

public sealed class DetalheModel : PageModel
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly IAtaService _ataService;

    public DetalheModel(IAtaService ataService)
    {
        _ataService = ataService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public AtaDetalheDto? Ata { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!User.PodeAcessarAtas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para acessar as Atas.";
            return RedirectToPage("/Index");
        }

        Ata = await _ataService.ObterDetalheAsync(Id, cancellationToken);
        if (Ata is null)
        {
            TempData["ErrorMessage"] = "Ata não encontrada.";
            return RedirectToPage("/Atas/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSalvarMatriculaAsync(Guid ataAlunoId, string? matriculaPrefeitura, CancellationToken cancellationToken)
    {
        var resultado = await _ataService.SalvarMatriculaAsync(ataAlunoId, matriculaPrefeitura, cancellationToken);
        return new JsonResult(new { succeeded = resultado.Succeeded, error = resultado.ErrorMessage });
    }

    public async Task<IActionResult> OnPostSalvarAvFAsync(Guid ataAlunoId, string? valor, CancellationToken cancellationToken)
    {
        if (!DecimalParsing.TryParseNota(valor, out var nota))
        {
            return new JsonResult(new { succeeded = false, error = "Formato de nota inválido. Use um valor entre 0 e 10." });
        }

        // Muda o RF (e, com isso, se Finalizar/Gerar PDF ficam liberados) — o cliente recarrega a página
        // em caso de sucesso para refletir isso, em vez de tentar atualizar tudo via JS.
        var resultado = await _ataService.SalvarAvFAsync(ataAlunoId, nota, cancellationToken);
        return new JsonResult(new { succeeded = resultado.Succeeded, error = resultado.ErrorMessage });
    }

    public async Task<IActionResult> OnPostDefinirRFAsync(Guid ataAlunoId, string rf, CancellationToken cancellationToken)
    {
        var resultado = await _ataService.DefinirRFManualAsync(ataAlunoId, rf, cancellationToken);
        return new JsonResult(new { succeeded = resultado.Succeeded, error = resultado.ErrorMessage });
    }

    public async Task<IActionResult> OnPostFinalizarAsync(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _ataService.FinalizarAsync(id, cancellationToken);
        TempData[resultado.Succeeded ? "SuccessMessage" : "ErrorMessage"] = resultado.Succeeded
            ? "Ata finalizada com sucesso."
            : resultado.ErrorMessage ?? "Não foi possível finalizar a Ata.";
        return RedirectToPage("/Atas/Detalhe", new { id });
    }

    public async Task<IActionResult> OnPostHabilitarEdicaoAsync(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _ataService.HabilitarEdicaoAsync(id, cancellationToken);
        TempData[resultado.Succeeded ? "SuccessMessage" : "ErrorMessage"] = resultado.Succeeded
            ? "Ata reaberta para edição."
            : resultado.ErrorMessage ?? "Não foi possível reabrir a Ata.";
        return RedirectToPage("/Atas/Detalhe", new { id });
    }

    public async Task<IActionResult> OnGetPdfAtaAsync(CancellationToken cancellationToken)
    {
        var ata = await _ataService.ObterDetalheAsync(Id, cancellationToken);
        if (ata is null)
        {
            TempData["ErrorMessage"] = "Ata não encontrada.";
            return RedirectToPage("/Atas/Index");
        }

        if (!ata.TodosComResultado)
        {
            TempData["ErrorMessage"] = "Ainda há aluno(s) sem resultado final definido.";
            return RedirectToPage("/Atas/Detalhe", new { id = Id });
        }

        var pdfBytes = GerarPdf(ata);
        var nomeArquivo = $"ata-{ata.TurmaNome}-{ata.AnoLetivo}.pdf".Replace(" ", "-").ToLowerInvariant();
        return File(pdfBytes, "application/pdf", nomeArquivo);
    }

    private static byte[] GerarPdf(AtaDetalheDto ata)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(esquerda =>
                        {
                            esquerda.Item().Text("Escola Municipal 2 de Julho").Bold().FontSize(13);
                            esquerda.Item().Text("Secretaria Municipal de Educação - Salvador, Bahia");
                            esquerda.Item().Text("Ata de Resultados Finais");
                        });

                        row.RelativeItem().Column(direita =>
                        {
                            direita.Item().AlignRight().Text($"{ata.TurmaNome}").Bold().FontSize(12);
                            direita.Item().AlignRight().Row(pills =>
                            {
                                pills.Spacing(4);
                                pills.AutoItem().Element(PillStyle).Text(ata.SerieNome);
                                pills.AutoItem().Element(PillStyle).Text($"Ano letivo {ata.AnoLetivo}");
                                if (ata.FinalizadaEmUtc.HasValue)
                                {
                                    pills.AutoItem().Element(PillStyle).Text($"Finalizada em {ata.FinalizadaEmUtc.Value:dd/MM/yyyy}");
                                }
                            });
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(20);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(2.6f);
                        foreach (var _ in ata.DisciplinasColunas)
                        {
                            columns.RelativeColumn(0.9f);
                        }

                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(1f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CabecalhoPrincipal).AlignMiddle().Text("Nº");
                        header.Cell().Element(CabecalhoPrincipal).AlignMiddle().Text("Matrícula");
                        header.Cell().Element(CabecalhoPrincipal).AlignMiddle().Text("Nome");
                        foreach (var coluna in ata.DisciplinasColunas)
                        {
                            header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle()
                                .Text(string.IsNullOrWhiteSpace(coluna.Codigo) ? coluna.Nome : coluna.Codigo);
                        }

                        header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("TP");
                        header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("MC");
                        header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("AvF");
                        header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("RF");
                    });

                    foreach (var item in ata.Itens)
                    {
                        table.Cell().Element(Celula).AlignMiddle().Text(item.Numero.ToString());
                        table.Cell().Element(Celula).AlignMiddle().Text(item.MatriculaPrefeitura ?? "—");
                        table.Cell().Element(Celula).AlignMiddle().Text(item.NomeCompleto);

                        foreach (var coluna in ata.DisciplinasColunas)
                        {
                            var disciplina = item.Disciplinas.FirstOrDefault(d => d.DisciplinaId == coluna.DisciplinaId);
                            table.Cell().Element(Celula).AlignCenter().AlignMiddle()
                                .Text(disciplina is null ? "—" : disciplina.ResultadoFinalAno.ToString("0.0", PtBr));
                        }

                        table.Cell().Element(Celula).AlignCenter().AlignMiddle().Text(item.TP.ToString("0.0", PtBr));
                        table.Cell().Element(Celula).AlignCenter().AlignMiddle().Text(item.MC.ToString("0.0", PtBr));
                        table.Cell().Element(Celula).AlignCenter().AlignMiddle().Text(item.AvF.HasValue ? item.AvF.Value.ToString("0.0", PtBr) : "—");
                        table.Cell().Element(CelulaRF(item.RF)).AlignCenter().AlignMiddle().Text(item.RF).Bold();
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static IContainer PillStyle(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).PaddingVertical(2).PaddingHorizontal(6).DefaultTextStyle(x => x.FontSize(8).SemiBold());

    private static IContainer CabecalhoPrincipal(IContainer container) =>
        container.Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).DefaultTextStyle(x => x.Bold().FontSize(7.5f));

    private static IContainer Celula(IContainer container) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).DefaultTextStyle(x => x.FontSize(7));

    private static Func<IContainer, IContainer> CelulaRF(string rf) => container =>
    {
        var corFundo = rf switch
        {
            AtaConstantes.RFAprovado => Colors.Green.Lighten4,
            AtaConstantes.RFConservado => Colors.Red.Lighten4,
            AtaConstantes.RFTransferido => Colors.Grey.Lighten3,
            _ => Colors.Amber.Lighten4
        };

        return container.Background(corFundo).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).DefaultTextStyle(x => x.FontSize(7));
    };
}
