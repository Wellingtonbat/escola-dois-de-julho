using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Atas;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages.Atas;

public sealed class DetalheModel : PageModel
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly IAtaService _ataService;
    private readonly IWebHostEnvironment _environment;

    public DetalheModel(IAtaService ataService, IWebHostEnvironment environment)
    {
        _ataService = ataService;
        _environment = environment;
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

        var brasaoPath = Path.Combine(_environment.WebRootPath, "pdf-assets", "brasao-prefeitura-salvador.jpg");
        var brasaoBytes = System.IO.File.ReadAllBytes(brasaoPath);
        var pdfBytes = GerarPdf(ata, brasaoBytes);
        var nomeArquivo = $"ata-{ata.TurmaNome}-{ata.AnoLetivo}.pdf".Replace(" ", "-").ToLowerInvariant();
        return File(pdfBytes, "application/pdf", nomeArquivo);
    }

    private const string EscolaCodigoNome = "1046 - ESCOLA MUNICIPAL 2 DE JULHO";

    private static byte[] GerarPdf(AtaDetalheDto ata, byte[] brasaoBytes)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8));

                // A logo da prefeitura repete em todas as páginas; o restante do cabeçalho (texto,
                // campos de preenchimento, parágrafo) aparece só uma vez, na primeira página, via
                // ShowOnce — evita repetir o bloco inteiro em Atas de turmas grandes com várias páginas.
                page.Header().Column(col =>
                {
                    // O texto "Secretaria da Educação" fica numa camada por cima (Layers), sobre um
                    // espaço reservado sempre presente na Row de baixo — assim a logo não "anda" para
                    // a esquerda nas páginas em que o texto (ShowOnce) deixa de ser desenhado.
                    col.Item().Layers(layers =>
                    {
                        layers.PrimaryLayer().Row(row =>
                        {
                            row.ConstantItem(100);
                            row.ConstantItem(90).Image(brasaoBytes).FitWidth();

                            row.RelativeItem().ShowOnce().AlignRight().AlignMiddle().Column(direita =>
                            {
                                if (ata.FinalizadaEmUtc.HasValue)
                                {
                                    direita.Item().AlignRight().Text($"Finalizada em {HorarioBrasilia.DeUtc(ata.FinalizadaEmUtc.Value):dd/MM/yyyy}")
                                        .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                                }
                                direita.Item().AlignRight().Text("ATA DE RESULTADOS FINAIS").Bold().FontSize(13);
                            });
                        });

                        layers.Layer().ShowOnce().Width(100).AlignMiddle().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(11).Bold().FontColor(Colors.Blue.Darken2));
                            t.Line("Secretaria da");
                            t.Line("Educação");
                        });
                    });

                    col.Item().ShowOnce().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(9));
                            t.Span("Escola: ").Bold();
                            t.Span(EscolaCodigoNome);
                        });
                        row.RelativeItem().AlignLeft().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(9));
                            t.Span("Diretor(a): ").Bold();
                            t.Span(ata.DiretorNome ?? string.Empty);
                        });
                    });

                    col.Item().ShowOnce().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(9));
                            t.Span("Coordenador(a) Pedagógico(a): ").Bold();
                        });
                        row.RelativeItem().AlignLeft().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(9));
                            t.Span("Professor(a): ").Bold();
                        });
                    });

                    col.Item().ShowOnce().PaddingTop(8).Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(9));
                        t.Span("Ao(s) ");
                        t.Span("_______").Underline();
                        t.Span(" dia(s) do mês de ");
                        t.Span("_______________").Underline();
                        t.Span(" do ano de ");
                        t.Span(ata.AnoLetivo.ToString(CultureInfo.InvariantCulture)).Bold();
                        t.Span(", encerrou-se o ano letivo programado para a turma do ");
                        t.Span(ata.TurmaNome).Bold();
                        t.Span(", do Ensino Fundamental II Regular, turno ");
                        t.Span(ata.Turno).Bold();
                        t.Span(", com os resultados constantes abaixo:");
                    });

                    col.Item().ShowOnce().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(20).Column(conteudo =>
                {
                    conteudo.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(20);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(3f);
                            foreach (var _ in ata.DisciplinasColunas)
                            {
                                columns.RelativeColumn(0.75f);
                            }

                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(0.9f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CabecalhoPrincipal).AlignMiddle().Text("Nº");
                            header.Cell().Element(CabecalhoPrincipal).AlignMiddle().Text("Matrícula");
                            header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("Nome");
                            foreach (var coluna in ata.DisciplinasColunas)
                            {
                                header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text(RotuloDisciplina(coluna));
                            }

                            header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("Resultado");
                            header.Cell().Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("Apto a Cursar");
                        });

                        foreach (var item in ata.Itens)
                        {
                            table.Cell().Element(Celula).AlignMiddle().Text(item.Numero.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(Celula).AlignMiddle().Text(item.MatriculaPrefeitura ?? string.Empty);
                            table.Cell().Element(Celula).AlignMiddle().Text(item.NomeCompleto.ToUpper(PtBr));

                            foreach (var coluna in ata.DisciplinasColunas)
                            {
                                // Transferido(a)/Deixou de frequentar: linha sem notas, como no modelo oficial.
                                var disciplina = item.Disciplinas.FirstOrDefault(d => d.DisciplinaId == coluna.DisciplinaId);
                                var texto = item.RFManual ? string.Empty : disciplina is null ? "—" : disciplina.ResultadoFinalAno.ToString("0.0", PtBr);
                                table.Cell().Element(Celula).AlignCenter().AlignMiddle().Text(texto);
                            }

                            table.Cell().Element(Celula).AlignCenter().AlignMiddle().Text(item.RFDescricao);
                            table.Cell().Element(Celula).AlignCenter().AlignMiddle().Text(item.AptoACursar ?? string.Empty);
                        }
                    });

                    // Fechamento da Ata (modelo oficial): fica inteiro na última página, logo após a tabela.
                    conteudo.Item().PaddingTop(16).ShowEntire().Element(Fechamento);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem();
                    row.RelativeItem().AlignCenter().Text($"Emitido em {HorarioBrasilia.Agora:dd/MM/yyyy}").FontSize(8);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.DefaultTextStyle(t => t.FontSize(8));
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer CabecalhoPrincipal(IContainer container) =>
        container.Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).DefaultTextStyle(x => x.Bold().FontSize(7.5f));

    private static IContainer Celula(IContainer container) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).DefaultTextStyle(x => x.FontSize(7));

    // LEM e Componente Optativo levam asterisco no cabeçalho, remetendo às linhas de rodapé da Ata.
    private static string RotuloDisciplina(AtaDisciplinaColunaDto coluna)
    {
        var rotulo = string.IsNullOrWhiteSpace(coluna.Codigo) ? coluna.Nome : coluna.Codigo;
        var ehLem = coluna.Nome.Contains("Estrangeira", StringComparison.OrdinalIgnoreCase)
            || coluna.Codigo.Equals("LEM", StringComparison.OrdinalIgnoreCase);
        var ehOptativa = coluna.Nome.Contains("Optativ", StringComparison.OrdinalIgnoreCase)
            || coluna.Codigo.Contains("Optativ", StringComparison.OrdinalIgnoreCase);
        return ehLem || ehOptativa ? $"{rotulo}*" : rotulo;
    }

    // Última parte da Ata, igual ao modelo da Secretaria: rodapés de LEM/Optativo, termo do(a) Secretário(a),
    // assinaturas, legenda e o quadro "Movimento Escolar" — tudo em branco, para preenchimento à caneta.
    private static void Fechamento(IContainer container)
    {
        container.DefaultTextStyle(x => x.FontSize(9)).Column(col =>
        {
            col.Spacing(6);
            col.Item().Text("*Língua Estrangeira Moderna - LEM: ________________________________");
            col.Item().Text("*Componente Optativo: ________________________________");
            col.Item().PaddingTop(4).Text(
                "E, para constar, eu, ____________________________________________, Secretário(a), lavrarei a presente ata " +
                "que vai assinada também pelo(a) Diretor(a) da Unidade de Ensino.");

            col.Item().PaddingTop(24).Row(row =>
            {
                row.RelativeItem().PaddingHorizontal(30).Column(assinatura =>
                {
                    assinatura.Item().LineHorizontal(0.8f);
                    assinatura.Item().AlignCenter().Text("Diretor(a)").Bold();
                });
                row.RelativeItem().PaddingHorizontal(30).Column(assinatura =>
                {
                    assinatura.Item().LineHorizontal(0.8f);
                    assinatura.Item().AlignCenter().Text("Chefe de Secretaria Escolar").Bold();
                });
            });

            col.Item().PaddingTop(16).Row(row =>
            {
                row.RelativeItem(1).Column(legenda =>
                {
                    legenda.Item().Text("Legenda").Bold();
                    legenda.Item().Text("PD - Parecer Descritivo");
                    legenda.Item().Text("PP - Aprovado(a) para Estudos em Regime de Progressão Parcial");
                });

                row.RelativeItem(1.4f).Table(quadro =>
                {
                    quadro.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(0.5f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(0.5f);
                        c.RelativeColumn(1f);
                        c.RelativeColumn(0.5f);
                    });

                    quadro.Cell().RowSpan(2).Element(CelulaQuadro).AlignMiddle().Text("Movimento Escolar:");
                    foreach (var rotulo in new[] { "Matrícula Inicial:", "Deixou de frequentar:", "Aprovação:" })
                    {
                        quadro.Cell().Element(CelulaQuadro).Text(rotulo);
                        quadro.Cell().Element(CelulaQuadro).Text(string.Empty);
                    }

                    foreach (var rotulo in new[] { "Transferência:", "Matrícula Final:", "Reprovação:" })
                    {
                        quadro.Cell().Element(CelulaQuadro).Text(rotulo);
                        quadro.Cell().Element(CelulaQuadro).Text(string.Empty);
                    }
                });
            });
        });
    }

    private static IContainer CelulaQuadro(IContainer container) =>
        container.Border(0.8f).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(8));
}
