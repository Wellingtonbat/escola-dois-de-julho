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
        Guid? turmaId,
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
                new AlunoCreateRequest(row.Cpf, row.Nome, row.DataNascimento, anoLetivo, serieId, turmaId, row.IsAtivo),
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
        worksheet.Cell(2, 3).Value = "25/12/2012";
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

            if (partes.Length < 3 || !TryBuildImportRow(partes[0], partes[1], ParseDate(partes[2]), partes.ElementAtOrDefault(3), out var row))
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

            // Quando a célula de nascimento é uma data "de verdade" (não texto digitado), lemos o valor
            // diretamente, sem passar por texto — isso evita qualquer ambiguidade de dia/mês por causa da
            // cultura do servidor (o container de produção usa formato americano por padrão).
            var celulaData = row.Cell(3);
            var dataNascimento = celulaData.DataType == XLDataType.DateTime
                ? celulaData.GetDateTime()
                : ParseDate(celulaData.GetString());

            if (!TryBuildImportRow(row.Cell(1).GetString(), row.Cell(2).GetString(), dataNascimento, row.Cell(4).GetString(), out var importRow))
            {
                falhas++;
                continue;
            }

            rows.Add(importRow!);
        }

        return (rows, falhas);
    }

    private static bool TryBuildImportRow(string? cpfRaw, string? nomeRaw, DateTime dataNascimento, string? statusRaw, out ImportAlunoRow? row)
    {
        row = null;

        var cpf = new string((cpfRaw ?? string.Empty).Where(char.IsDigit).ToArray());
        var nome = (nomeRaw ?? string.Empty).Trim();
        var isAtivo = ParseStatus(statusRaw);

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

    // Formatos aceitos, nessa ordem, sempre com dia antes do mês (padrão brasileiro). Usamos TryParseExact
    // com CultureInfo.InvariantCulture em vez de TryParse: assim o resultado nunca depende da cultura
    // configurada no servidor (o container de produção usa formato americano por padrão, o que trocaria
    // dia por mês silenciosamente em datas como 05/10/2012).
    private static readonly string[] FormatosDataAceitos =
    {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "yyyy-MM-dd",
    };

    private static DateTime ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return DateTime.TryParseExact(
            value.Trim(),
            FormatosDataAceitos,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var parsed)
            ? parsed.Date
            : default;
    }

    private sealed record ImportAlunoRow(
        string Cpf,
        string Nome,
        DateTime DataNascimento,
        bool IsAtivo);
}
