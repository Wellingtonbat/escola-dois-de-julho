namespace SistemaEscolar.Application.Atas;

public static class AtaConstantes
{
    public const string StatusRascunho = "Rascunho";
    public const string StatusFinalizada = "Finalizada";

    // RF (Resultado Final) de cada aluno na Ata.
    public const string RFPendente = "Pendente";
    public const string RFAprovado = "Aprovado";
    public const string RFConservado = "Conservado";
    public const string RFTransferido = "Transferido";

    public static readonly IReadOnlyList<string> ValoresRF = new[] { RFPendente, RFAprovado, RFConservado, RFTransferido };

    public const decimal MediaAprovacao = 5.0m;
}
