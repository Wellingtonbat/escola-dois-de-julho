using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;
using System.Text;

namespace SistemaEscolar.Web.Pages.Resultados;

public sealed class IndexModel : PageModel
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private const int PageSizeFixo = 50;
    private readonly IResultadoAcademicoService _resultadoService;
    private readonly IRecuperacaoFinalService _recuperacaoFinalService;
    private readonly ITurmaService _turmaService;
    private readonly ISerieService _serieService;
    private readonly INotaService _notaService;
    private readonly IPeriodoService _periodoService;

    public IndexModel(
        IResultadoAcademicoService resultadoService,
        IRecuperacaoFinalService recuperacaoFinalService,
        ITurmaService turmaService,
        ISerieService serieService,
        INotaService notaService,
        IPeriodoService periodoService)
    {
        _resultadoService = resultadoService;
        _recuperacaoFinalService = recuperacaoFinalService;
        _turmaService = turmaService;
        _serieService = serieService;
        _notaService = notaService;
        _periodoService = periodoService;
    }

    public IReadOnlyList<ResultadoAcademicoDto> Resultados { get; private set; } = Array.Empty<ResultadoAcademicoDto>();
    public IReadOnlyList<SelectListItem> Turmas { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Series { get; private set; } = Array.Empty<SelectListItem>();

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
        await LoadFilterOptionsAsync(cancellationToken);

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

        var periodosDoAno = (await _periodoService.ListarAsync(null, cancellationToken))
            .Where(x => x.AnoLetivo == AnoLetivo)
            .OrderBy(x => x.Trimestre)
            .ToList();

        var todasNotas = await _notaService.ListarAsync(null, cancellationToken);
        var recuperacoesFinais = await _recuperacaoFinalService.ListarPorAnoAsync(AnoLetivo, cancellationToken);

        using var workbook = new XLWorkbook();

        var gruposDisciplina = resultados
            .GroupBy(x => x.Disciplina)
            .OrderBy(x => x.Key);

        foreach (var grupo in gruposDisciplina)
        {
            var worksheet = workbook.Worksheets.Add(SanitizarNomeAba(grupo.Key));

            worksheet.Cell(1, 1).Value = "Aluno";
            worksheet.Range(1, 1, 2, 1).Merge();

            var colunasSub = new[] { "Av1", "Av2", "Av3", "Res.Un", "Rec.Par", "Res.Fi" };
            var coluna = 2;
            for (var t = 1; t <= 3; t++)
            {
                worksheet.Range(1, coluna, 1, coluna + 5).Merge().Value = $"{t}º Trimestre";
                for (var i = 0; i < colunasSub.Length; i++)
                {
                    worksheet.Cell(2, coluna + i).Value = colunasSub[i];
                }

                coluna += 6;
            }

            worksheet.Cell(1, coluna).Value = "Tot.Pts";
            worksheet.Range(1, coluna, 2, coluna).Merge();
            worksheet.Cell(1, coluna + 1).Value = "Méd.Curso";
            worksheet.Range(1, coluna + 1, 2, coluna + 1).Merge();
            worksheet.Cell(1, coluna + 2).Value = "Rec.";
            worksheet.Range(1, coluna + 2, 2, coluna + 2).Merge();
            worksheet.Cell(1, coluna + 3).Value = "Res.";
            worksheet.Range(1, coluna + 3, 2, coluna + 3).Merge();
            var totalColunas = coluna + 3;

            var cabecalho = worksheet.Range(1, 1, 2, totalColunas);
            cabecalho.Style.Font.Bold = true;
            cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            cabecalho.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cabecalho.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cabecalho.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var linha = 3;
            foreach (var item in grupo.OrderBy(x => x.AlunoNome))
            {
                var notasDoAluno = todasNotas
                    .Where(n => n.AlunoId == item.AlunoId && n.DisciplinaId == item.DisciplinaId)
                    .ToList();
                var temRecuperacao = recuperacoesFinais.TryGetValue((item.AlunoId, item.DisciplinaId), out var recuperacaoValor);
                var calculo = BoletimCalculo.Calcular(periodosDoAno, notasDoAluno, temRecuperacao ? recuperacaoValor : null);

                worksheet.Cell(linha, 1).Value = item.AlunoNome;

                var c = 2;
                foreach (var trimestre in calculo.Trimestres)
                {
                    worksheet.Cell(linha, c).Value = trimestre.Av1;
                    worksheet.Cell(linha, c + 1).Value = trimestre.Av2;
                    worksheet.Cell(linha, c + 2).Value = trimestre.Av3;
                    worksheet.Cell(linha, c + 3).Value = trimestre.ResUnidade;
                    worksheet.Cell(linha, c + 4).Value = trimestre.RecParalela;
                    worksheet.Cell(linha, c + 5).Value = trimestre.ResFinal;
                    c += 6;
                }

                worksheet.Cell(linha, c).Value = BoletimCalculo.FormatarNumero(calculo.TotalPontos);
                worksheet.Cell(linha, c + 1).Value = calculo.MediaCurso.HasValue ? BoletimCalculo.FormatarNumero(calculo.MediaCurso.Value) : "—";
                worksheet.Cell(linha, c + 2).Value = calculo.RecuperacaoFinal.HasValue ? BoletimCalculo.FormatarNumero(calculo.RecuperacaoFinal.Value) : "—";

                var celulaSituacao = worksheet.Cell(linha, c + 3);
                celulaSituacao.Value = calculo.Situacao;
                if (calculo.Situacao == "AP")
                {
                    celulaSituacao.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDF8EC");
                }
                else if (calculo.Situacao == "RP")
                {
                    celulaSituacao.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE3EA");
                }

                linha++;
            }

            if (linha > 3)
            {
                var corpo = worksheet.Range(3, 1, linha - 1, totalColunas);
                corpo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                corpo.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.SheetView.FreezeRows(2);
            worksheet.Columns().AdjustToContents();
        }

        if (workbook.Worksheets.Count == 0)
        {
            workbook.Worksheets.Add("Resultados").Cell(1, 1).Value = "Nenhum resultado encontrado.";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"resultados-academicos-{AnoLetivo}.xlsx");
    }

    private static string SanitizarNomeAba(string nome)
    {
        var invalidos = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var limpo = new string(nome.Select(c => invalidos.Contains(c) ? '-' : c).ToArray());
        return limpo.Length > 31 ? limpo[..31] : limpo;
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

    private async Task LoadFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var turmas = await _turmaService.ListarAsync(null, cancellationToken);
        Turmas = turmas
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem($"{x.Nome} ({x.SerieNome})", x.Nome))
            .ToList();

        var series = await _serieService.ListarAsync(null, cancellationToken);
        Series = series
            .OrderBy(x => x.Ordem)
            .Select(x => new SelectListItem(x.Nome, x.Nome))
            .ToList();
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
                            || x.ProfessorNome.Contains(busca, StringComparison.OrdinalIgnoreCase))
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
