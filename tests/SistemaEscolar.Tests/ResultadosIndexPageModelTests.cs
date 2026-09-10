using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Tests.Support;
using SistemaEscolar.Web.Pages.Resultados;
using UglyToad.PdfPig;

namespace SistemaEscolar.Tests;

public sealed class ResultadosIndexPageModelTests
{
    [Fact]
    public async Task OnGetAsync_DeveAplicarOrdenacaoPorMediaEDepoisPaginar()
    {
        var data = Enumerable.Range(1, 11)
            .Select(i => new ResultadoAcademicoDto(
                Guid.NewGuid(),
                $"Aluno {i:00}",
                Guid.NewGuid(),
                "Matemática",
                "8o Ano A",
                "8o Ano",
                "Professora Ana",
                2026,
                i,
                null,
                false,
                i,
                "Aprovado",
                "Aprovado por média final."))
            .ToList();

        var model = BuildModel(data);
        model.Ordenacao = "media";
        model.Direcao = "desc";
        model.PageSize = 10;
        model.PageNumber = 2;

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(11, model.TotalCount);
        Assert.Equal(2, model.TotalPages);
        Assert.Single(model.Resultados);
        Assert.Equal("Aluno 01", model.Resultados[0].AlunoNome);
    }

    [Fact]
    public async Task ExportCsv_DeveRetornarArquivoCsv()
    {
        var model = BuildModel(AmostraPadrao());

        var actionResult = await model.OnGetExportCsvAsync(CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.EndsWith(".csv", fileResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(fileResult.FileContents);
    }

    [Fact]
    public async Task ExportExcel_DeveGerarUmaAbaPorDisciplinaComCabecalhoDeTrimestres()
    {
        var model = BuildModel(AmostraPadrao());

        var actionResult = await model.OnGetExportExcelAsync(CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.EndsWith(".xlsx", fileResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream(fileResult.FileContents);
        using var workbook = new XLWorkbook(stream);

        Assert.Contains("Matemática", workbook.Worksheets.Select(w => w.Name));
        Assert.Contains("Português", workbook.Worksheets.Select(w => w.Name));

        var matematica = workbook.Worksheet("Matemática");
        Assert.Equal("Aluno", matematica.Cell(1, 1).GetString());
        Assert.Equal("1º Trimestre", matematica.Cell(1, 2).GetString());
        Assert.Equal("Av1", matematica.Cell(2, 2).GetString());
        Assert.Equal("Tot.Pts", matematica.Cell(1, 20).GetString());
        Assert.Equal("Aluno A", matematica.Cell(3, 1).GetString());
    }

    [Fact]
    public async Task ExportPdf_DeveConterTotaisPorSituacao()
    {
        var model = BuildModel(AmostraPadrao());

        var actionResult = await model.OnGetExportPdfAsync(CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.EndsWith(".pdf", fileResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream(fileResult.FileContents);
        using var pdf = PdfDocument.Open(stream);
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Relatório de Resultados Acadêmicos", text);
        Assert.Contains("Total de registros", text);
        Assert.Contains("Aprovados", text);
        Assert.Contains("Reprovados", text);
        Assert.Contains("Pendentes", text);
    }

    private static List<ResultadoAcademicoDto> AmostraPadrao() =>
        new()
        {
            new(Guid.NewGuid(), "Aluno A", Guid.NewGuid(), "Matemática", "8o Ano A", "8o Ano", "Professor João", 2026, 8.25m, null, false, 8.25m, "Aprovado", "Aprovado por média final."),
            new(Guid.NewGuid(), "Aluno B", Guid.NewGuid(), "Matemática", "8o Ano A", "8o Ano", "Professor João", 2026, 5.25m, null, false, 5.25m, "Reprovado", "Média final abaixo de 5,0."),
            new(Guid.NewGuid(), "Aluno C", Guid.NewGuid(), "Português", "9o Ano B", "9o Ano", "Professora Ana", 2026, 0m, null, false, 0m, "Pendente", "Lançamentos incompletos no ano letivo.")
        };

    private static IndexModel BuildModel(IReadOnlyList<ResultadoAcademicoDto> data)
    {
        var service = new ResultadoAcademicoServiceStub(data);
        return new IndexModel(
            service,
            new RecuperacaoFinalServiceStub(),
            new TurmaServiceStub(),
            new SerieServiceStub(),
            new NotaServiceStub(),
            new PeriodoServiceStub())
        {
            AnoLetivo = 2026,
            Situacao = "todos",
            Ordenacao = "aluno",
            Direcao = "asc"
        };
    }
}
