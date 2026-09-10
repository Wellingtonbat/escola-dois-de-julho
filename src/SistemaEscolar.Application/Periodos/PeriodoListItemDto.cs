namespace SistemaEscolar.Application.Periodos;

public sealed record PeriodoListItemDto(
    Guid Id,
    int AnoLetivo,
    int Trimestre,
    string Descricao,
    DateTime DataInicial,
    DateTime DataFinal,
    bool IsAberto,
    bool AbertoManualmente);
