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
                "Matemática",
                "8o Ano A",
                "8o Ano",
                2026,
                i,
                "Aprovado",
                "Aprovado por média final."))
            .ToList();

        var model = new IndexModel(new ResultadoAcademicoServiceStub(data));
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
        var model = BuildModel();

        var actionResult = await model.OnGetExportCsvAsync(CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.EndsWith(".csv", fileResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(fileResult.FileContents);
    }

    [Fact]
    public async Task ExportExcel_DeveRetornarArquivoXlsxComCabecalho()
    {
        var model = BuildModel();

        var actionResult = await model.OnGetExportExcelAsync(CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.EndsWith(".xlsx", fileResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream(fileResult.FileContents);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Resultados");

        Assert.Equal("Disciplina", sheet.Cell(1, 1).GetString());
        Assert.Equal("Situação", sheet.Cell(1, 7).GetString());
        Assert.Equal("Matemática", sheet.Cell(2, 1).GetString());
        Assert.Equal("Aluno A", sheet.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task ExportPdf_DeveConterTotaisPorSituacao()
    {
        var model = BuildModel();

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

    private static IndexModel BuildModel()
    {
        var data = new List<ResultadoAcademicoDto>
        {
            new(Guid.NewGuid(), "Aluno A", "Matemática", "8o Ano A", "8o Ano", 2026, 8.25m, "Aprovado", "Aprovado por média final."),
            new(Guid.NewGuid(), "Aluno B", "Matemática", "8o Ano A", "8o Ano", 2026, 5.25m, "Reprovado", "Média final abaixo de 5,0."),
            new(Guid.NewGuid(), "Aluno C", "Português", "9o Ano B", "9o Ano", 2026, 0m, "Pendente", "Lançamentos incompletos no ano letivo.")
        };

        var service = new ResultadoAcademicoServiceStub(data);
        return new IndexModel(service)
        {
            AnoLetivo = 2026,
            Situacao = "todos",
            Ordenacao = "aluno",
            Direcao = "asc"
        };
    }
}
