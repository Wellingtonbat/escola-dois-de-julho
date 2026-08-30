namespace SistemaEscolar.Application.Alunos;

public interface IAlunoImportService
{
    Task<AlunoImportResult> ImportarAsync(
        string fileName,
        Stream stream,
        Guid serieId,
        int anoLetivo,
        CancellationToken cancellationToken = default);

    Task<AlunoImportTemplate> GerarModeloExcelAsync(CancellationToken cancellationToken = default);
}
