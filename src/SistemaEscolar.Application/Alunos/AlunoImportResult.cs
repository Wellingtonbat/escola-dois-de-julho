namespace SistemaEscolar.Application.Alunos;

public sealed record AlunoImportResult(int Importados, int Falhas)
{
    public bool Success => Importados > 0;
}
