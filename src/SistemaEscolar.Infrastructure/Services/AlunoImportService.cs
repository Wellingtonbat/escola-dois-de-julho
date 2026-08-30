using ClosedXML.Excel;
using SistemaEscolar.Application.Alunos;

namespace SistemaEscolar.Infrastructure.Services;

public sealed class AlunoImportService : IAlunoImportService
{
    private readonly IAlunoService _alunoService;

    public AlunoImportService(IAlunoService alunoService)
    {
        _alunoService = alunoService;
    }

    public async Task<AlunoImportResult> ImportarAsync(
        string fileName,
        Stream stream,
        Guid serieId,
        int anoLetivo,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new AlunoImportResult(0, 1);
        }

        IReadOnlyList<ImportAlunoRow> rows;
        var falhas = 0;

        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            (rows, falhas) = ReadExcelRows(stream);
        }
        else
        {
            (rows, falhas) = await ReadCsvRowsAsync(stream, cancellationToken);
        }

        if (rows.Count > 5000)
        {
            return new AlunoImportResult(0, rows.Count);
        }

        var importados = 0;

        foreach (var row in rows)
        {
            var result = await _alunoService.CriarAsync(
                new AlunoCreateRequest(row.Cpf, row.Nome, row.DataNascimento, anoLetivo, serieId, null, row.IsAtivo),
                cancellationToken);

            if (result.Succeeded)
            {
                importados++;
            }
            else
            {
                falhas++;
            }
        }

        return new AlunoImportResult(importados, falhas);
    }

    public Task<AlunoImportTemplate> GerarModeloExcelAsync(CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Alunos");

        worksheet.Cell(1, 1).Value = "Cpf";
        worksheet.Cell(1, 2).Value = "Nome";
        worksheet.Cell(1, 3).Value = "DataNascimento";
        worksheet.Cell(1, 4).Value = "Status";

        worksheet.Cell(2, 1).Value = "12345678901";
        worksheet.Cell(2, 2).Value = "Maria Silva";
        worksheet.Cell(2, 3).Value = "10/05/2012";
        worksheet.Cell(2, 4).Value = "Ativo";

        worksheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
        worksheet.Columns(1, 4).AdjustToContents();

        using var memory = new MemoryStream();
        workbook.SaveAs(memory);

        return Task.FromResult(new AlunoImportTemplate(
            "modelo-importacao-alunos.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            memory.ToArray()));
    }

    private static async Task<(IReadOnlyList<ImportAlunoRow> Rows, int Falhas)> ReadCsvRowsAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        var rows = new List<ImportAlunoRow>();
        var falhas = 0;
        var numeroLinha = 0;

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            numeroLinha++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var partes = line.Contains(';') ? line.Split(';') : line.Split(',');
            if (partes.Length == 0)
            {
                continue;
            }

            if (numeroLinha == 1 && partes[0].Trim().Equals("cpf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryBuildImportRow(partes, out var row))
            {
                falhas++;
                continue;
            }

            rows.Add(row!);
        }

        return (rows, falhas);
    }

    private static (IReadOnlyList<ImportAlunoRow> Rows, int Falhas) ReadExcelRows(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<ImportAlunoRow>();
        var falhas = 0;

        foreach (var row in worksheet.RowsUsed())
        {
            var primeiraCelula = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(primeiraCelula))
            {
                continue;
            }

            if (primeiraCelula.Equals("cpf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var partes = new[]
            {
                row.Cell(1).GetString(),
                row.Cell(2).GetString(),
                row.Cell(3).GetString(),
                row.Cell(4).GetString()
            };

            if (!TryBuildImportRow(partes, out var importRow))
            {
                falhas++;
                continue;
            }

            rows.Add(importRow!);
        }

        return (rows, falhas);
    }

    private static bool TryBuildImportRow(string[] partes, out ImportAlunoRow? row)
    {
        row = null;

        if (partes.Length < 3)
        {
            return false;
        }

        var cpf = new string((partes[0] ?? string.Empty).Where(char.IsDigit).ToArray());
        var nome = partes[1].Trim();
        var dataNascimento = ParseDate(partes[2]);
        var isAtivo = partes.Length >= 4 ? ParseStatus(partes[3]) : true;

        if (cpf.Length != 11 || string.IsNullOrWhiteSpace(nome) || dataNascimento == default)
        {
            return false;
        }

        row = new ImportAlunoRow(cpf, nome, dataNascimento, isAtivo);
        return true;
    }

    private static bool ParseStatus(string? statusRaw)
    {
        if (string.IsNullOrWhiteSpace(statusRaw))
        {
            return true;
        }

        var status = statusRaw.Trim().ToLowerInvariant();
        return status is "ativo" or "1" or "true" or "sim";
    }

    private static DateTime ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed.Date;
        }

        return default;
    }

    private sealed record ImportAlunoRow(
        string Cpf,
        string Nome,
        DateTime DataNascimento,
        bool IsAtivo);
}
