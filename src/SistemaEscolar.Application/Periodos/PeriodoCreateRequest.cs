namespace SistemaEscolar.Application.Periodos;

public sealed record PeriodoCreateRequest(
    int AnoLetivo,
    int Trimestre,
    string Descricao,
    DateTime DataInicial,
    DateTime DataFinal,
    bool IsAberto);
