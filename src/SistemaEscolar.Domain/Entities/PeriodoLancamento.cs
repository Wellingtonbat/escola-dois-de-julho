using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class PeriodoLancamento : BaseEntity
{
    public Guid Id { get; set; }
    public int AnoLetivo { get; set; }
    public int Bimestre { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataInicial { get; set; }
    public DateTime DataFinal { get; set; }
    public bool IsAberto { get; set; }

    // Marca que o período foi aberto manualmente pelo botão (Diretor/Coordenador), ligando uma
    // exceção que ignora a janela de datas até que alguém feche o período de volta. Fora dessa
    // exceção, a disponibilidade volta a depender da janela DataInicial-DataFinal.
    public bool AbertoManualmente { get; set; }
}
