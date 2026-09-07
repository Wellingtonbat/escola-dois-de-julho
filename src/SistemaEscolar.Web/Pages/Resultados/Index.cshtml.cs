using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Resultados;
using System.Text;

namespace SistemaEscolar.Web.Pages.Resultados;

public sealed class IndexModel : PageModel
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private const int PageSizeFixo = 50;
    private readonly IResultadoAcademicoService _resultadoService;
    private readonly IRecuperacaoFinalService _recuperacaoFinalService;

    public IndexModel(IResultadoAcademicoService resultadoService, IRecuperacaoFinalService recuperacaoFinalService)
    {
        _resultadoService = resultadoService;
        _recuperacaoFinalService = recuperacaoFinalService;
    }

    public IReadOnlyList<ResultadoAcademicoDto> Resultados { get; private set; } = Array.Empty<ResultadoAcademicoDto>();

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public int AnoLetivo { get; set; } = DateTime.UtcNow.Year;

    [BindProperty(SupportsGet = true)]
    public string? Turma { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Serie { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Situacao { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public string Ordenacao { get; set; } = "aluno";

    [BindProperty(SupportsGet = true)]
    public string Direcao { get; set; } = "asc";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = PageSizeFixo;

    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalAprovados { get; private set; }
    public int TotalReprovados { get; private set; }
    public int TotalPendentes { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var resultados = await GetFilteredOrderedAsync(cancellationToken);

        TotalAprovados = resultados.Count(x => x.Situacao.Equals("Aprovado", StringComparison.OrdinalIgnoreCase));
        TotalReprovados = resultados.Count(x => x.Situacao.Equals("Reprovado", StringComparison.OrdinalIgnoreCase));
        TotalPendentes = resultados.Count(x => x.Situacao.Equals("Pendente", StringComparison.OrdinalIgnoreCase));

        TotalCount = resultados.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Resultados = resultados
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public async Task<IActionResult> OnPostSalvarRecuperacaoFinalAsync(
        Guid alunoId,
        Guid disciplinaId,
        string? recuperacaoFinal,
        string? busca,
        int anoLetivo,
        string? turma,
        string? serie,
        string? situacao,
        string? ordenacao,
        string? direcao,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        if (!CanLancarRecuperacao())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para lançar a Recuperação Final.";
            return RedirectToPageComFiltros(busca, anoLetivo, turma, serie, situacao, ordenacao, direcao, pageNumber);
        }

        if (!TryParseNota(recuperacaoFinal, out var valor) || !valor.HasValue)
        {
            TempData["ErrorMessage"] = "Informe um valor numérico entre 0 e 10 para a Recuperação Final.";
            return RedirectToPageComFiltros(busca, anoLetivo, turma, serie, situacao, ordenacao, direcao, pageNumber);
        }

        var resultado = await _recuperacaoFinalService.SalvarAsync(
            new RecuperacaoFinalSaveRequest(alunoId, disciplinaId, anoLetivo, valor.Value),
            cancellationToken);

        TempData[resultado.Succeeded ? "SuccessMessage" : "ErrorMessage"] = resultado.Succeeded
            ? "Recuperação Final lançada com sucesso."
            : resultado.ErrorMessage;

        return RedirectToPageComFiltros(busca, anoLetivo, turma, serie, situacao, ordenacao, direcao, pageNumber);
    }

    private IActionResult RedirectToPageComFiltros(
        string? busca, int anoLetivo, string? turma, string? serie, string? situacao, string? ordenacao, string? direcao, int pageNumber)
    {
        return RedirectToPage("/Resultados/Index", new
        {
            Busca = busca,
            AnoLetivo = anoLetivo,
            Turma = turma,
            Serie = serie,
            Situacao = situacao,
            Ordenacao = ordenacao,
            Direcao = direcao,
            PageNumber = pageNumber
        });
    }

    private bool CanLancarRecuperacao() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria") || User.IsInRole("Professor");

    private static bool TryParseNota(string? rawValue, out decimal? value)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            return false;
        }

        var text = rawValue.Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(text, NumberStyles.Number, PtBr, out parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    public async Task<IActionResult> OnGetExportCsvAsync(CancellationToken cancellationToken)
    {
        var resultados = await GetFilteredOrderedAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Disciplina;Aluno;Turma;Serie;AnoLetivo;MediaFinal;RecuperacaoFinal;ResultadoFinal;Situacao;Motivo");

        foreach (var item in resultados)
        {
            csv.AppendLine(string.Join(';',
            EscapeCsv(item.Disciplina),
                EscapeCsv(item.AlunoNome),
                EscapeCsv(item.Turma),
                EscapeCsv(item.Serie),
                item.AnoLetivo,
                item.MediaFinal.ToString("0.00"),
                item.RecuperacaoFinal.HasValue ? item.RecuperacaoFinal.Value.ToString("0.00") : "",
                item.ResultadoFinalAno.ToString("0.00"),
                EscapeCsv(item.Situacao),
                EscapeCsv(item.Motivo)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"resultados-academicos-{AnoLetivo}.csv");
    }

    public async Task<IActionResult> OnGetExportExcelAsync(CancellationToken cancellationToken)
    {
        var resultados = await GetFilteredOrderedAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Resultados");

        worksheet.Cell(1, 1).Value = "Disciplina";
        worksheet.Cell(1, 2).Value = "Aluno";
        worksheet.Cell(1, 3).Value = "Turma";
        worksheet.Cell(1, 4).Value = "Série";
        worksheet.Cell(1, 5).Value = "Ano Letivo";
        worksheet.Cell(1, 6).Value = "Média Final";
        worksheet.Cell(1, 7).Value = "Recuperação Final";
        worksheet.Cell(1, 8).Value = "Resultado Final";
        worksheet.Cell(1, 9).Value = "Situação";
        worksheet.Cell(1, 10).Value = "Motivo";

        var row = 2;
        foreach (var item in resultados)
        {
            worksheet.Cell(row, 1).Value = item.Disciplina;
            worksheet.Cell(row, 2).Value = item.AlunoNome;
            worksheet.Cell(row, 3).Value = item.Turma;
            worksheet.Cell(row, 4).Value = item.Serie;
            worksheet.Cell(row, 5).Value = item.AnoLetivo;
            worksheet.Cell(row, 6).Value = item.MediaFinal;
            if (item.RecuperacaoFinal.HasValue)
            {
                worksheet.Cell(row, 7).Value = item.RecuperacaoFinal.Value;
            }
            worksheet.Cell(row, 8).Value = item.ResultadoFinalAno;
            worksheet.Cell(row, 9).Value = item.Situacao;
            worksheet.Cell(row, 10).Value = item.Motivo;
            row++;
        }

        worksheet.Range(1, 1, 1, 10).Style.Font.Bold = true;
        worksheet.Column(6).Style.NumberFormat.Format = "0.00";
        worksheet.Column(7).Style.NumberFormat.Format = "0.00";
        worksheet.Column(8).Style.NumberFormat.Format = "0.00";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"resultados-academicos-{AnoLetivo}.xlsx");
    }

    public async Task<IActionResult> OnGetExportPdfAsync(CancellationToken cancellationToken)
    {
        var resultados = await GetFilteredOrderedAsync(cancellationToken);
        var totalAprovados = resultados.Count(x => x.Situacao.Equals("Aprovado", StringComparison.OrdinalIgnoreCase));
        var totalReprovados = resultados.Count(x => x.Situacao.Equals("Reprovado", StringComparison.OrdinalIgnoreCase));
        var totalPendentes = resultados.Count(x => x.Situacao.Equals("Pendente", StringComparison.OrdinalIgnoreCase));
        var disciplinas = resultados
            .GroupBy(x => x.Disciplina)
            .OrderBy(x => x.Key)
            .ToList();

        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Relatório de Resultados Acadêmicos").Bold().FontSize(14);
                    col.Item().Text($"Ano Letivo: {AnoLetivo} | Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}");
                });

                page.Content().Column(content =>
                {
                    content.Item().PaddingBottom(8).Text($"Aprovados: {totalAprovados} | Reprovados: {totalReprovados} | Pendentes: {totalPendentes}");

                    foreach (var disciplina in disciplinas)
                    {
                        var disciplinaAprovados = disciplina.Count(x => x.Situacao.Equals("Aprovado", StringComparison.OrdinalIgnoreCase));
                        var disciplinaReprovados = disciplina.Count(x => x.Situacao.Equals("Reprovado", StringComparison.OrdinalIgnoreCase));
                        var disciplinaPendentes = disciplina.Count(x => x.Situacao.Equals("Pendente", StringComparison.OrdinalIgnoreCase));

                        content.Item().PaddingTop(10).Text($"Disciplina: {disciplina.Key}").Bold().FontSize(11);

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(3.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Aluno").Bold();
                                header.Cell().Element(CellStyle).Text("Turma").Bold();
                                header.Cell().Element(CellStyle).Text("Série").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Ano").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Média").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Resultado Final").Bold();
                                header.Cell().Element(CellStyle).Text("Situação").Bold();
                                header.Cell().Element(CellStyle).Text("Motivo").Bold();
                            });

                            foreach (var item in disciplina.OrderBy(x => x.AlunoNome))
                            {
                                table.Cell().Element(CellStyle).Text(item.AlunoNome);
                                table.Cell().Element(CellStyle).Text(item.Turma);
                                table.Cell().Element(CellStyle).Text(item.Serie);
                                table.Cell().Element(CellStyle).AlignCenter().Text(item.AnoLetivo.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(item.MediaFinal.ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(item.ResultadoFinalAno.ToString("0.00"));
                                table.Cell().Element(CellStyle).Text(item.Situacao);
                                table.Cell().Element(CellStyle).Text(item.Motivo);
                            }
                        });

                        content.Item().PaddingBottom(4).Text($"Subtotal disciplina: {disciplina.Count()} | Aprovados: {disciplinaAprovados} | Reprovados: {disciplinaReprovados} | Pendentes: {disciplinaPendentes}");
                    }
                });

                page.Footer().AlignRight().Text($"Total de registros: {resultados.Count}");
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"resultados-academicos-{AnoLetivo}.pdf");
    }

    private async Task<List<ResultadoAcademicoDto>> GetFilteredOrderedAsync(CancellationToken cancellationToken)
    {
        var filter = new ResultadoAcademicoFilter(AnoLetivo, Turma, Serie, Situacao);
        var resultados = await _resultadoService.ListarAsync(filter, cancellationToken);

        if (!string.IsNullOrWhiteSpace(Busca))
        {
            var busca = Busca.Trim();
            resultados = resultados
                .Where(x => x.AlunoNome.Contains(busca, StringComparison.OrdinalIgnoreCase)
                            || x.Disciplina.Contains(busca, StringComparison.OrdinalIgnoreCase)
                            || x.Turma.Contains(busca, StringComparison.OrdinalIgnoreCase)
                            || x.Serie.Contains(busca, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return ApplyOrdering(resultados);
    }

    private List<ResultadoAcademicoDto> ApplyOrdering(IReadOnlyList<ResultadoAcademicoDto> resultados)
    {
        Func<ResultadoAcademicoDto, object> keySelector = Ordenacao.ToLowerInvariant() switch
        {
            "disciplina" => x => x.Disciplina,
            "media" => x => x.MediaFinal,
            "turma" => x => x.Turma,
            "situacao" => x => x.Situacao,
            _ => x => x.AlunoNome
        };

        IOrderedEnumerable<ResultadoAcademicoDto> ordered;
        if (string.Equals(Ordenacao, "aluno", StringComparison.OrdinalIgnoreCase))
        {
            ordered = string.Equals(Direcao, "desc", StringComparison.OrdinalIgnoreCase)
                ? resultados.OrderByDescending(x => x.Disciplina).ThenByDescending(x => x.AlunoNome)
                : resultados.OrderBy(x => x.Disciplina).ThenBy(x => x.AlunoNome);
        }
        else
        {
            ordered = string.Equals(Direcao, "desc", StringComparison.OrdinalIgnoreCase)
                ? resultados.OrderByDescending(keySelector).ThenBy(x => x.AlunoNome)
                : resultados.OrderBy(keySelector).ThenBy(x => x.AlunoNome);
        }

        return ordered.ToList();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .PaddingVertical(4)
            .PaddingHorizontal(3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);
    }
}
