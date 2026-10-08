namespace SistemaEscolar.Application.Atas;

public static class AtaConstantes
{
    public const string StatusRascunho = "Rascunho";
    public const string StatusFinalizada = "Finalizada";

    // Resultado de cada aluno na Ata (coluna "Resultado"). Pendente/Aprovado/Conservado são calculados pelo
    // sistema; Transferido e Deixou de frequentar são marcados à mão (aluno inativo já começa Transferido).
    public const string RFPendente = "Pendente";
    public const string RFAprovado = "Aprovado";
    public const string RFConservado = "Conservado";
    public const string RFTransferido = "Transferido";
    public const string RFDeixouDeFrequentar = "DeixouDeFrequentar";

    // Valor enviado pela tela para voltar ao resultado calculado pelo sistema (não é gravado).
    public const string RFAutomatico = "Automatico";

    public static readonly IReadOnlyList<string> ValoresRFManuais = new[] { RFTransferido, RFDeixouDeFrequentar };

    public static bool EhRFManual(string rf) => ValoresRFManuais.Contains(rf);

    public static string DescreverRF(string rf) => rf switch
    {
        RFAprovado => "Aprovado(a)",
        RFConservado => "Conservado(a)",
        RFTransferido => "Transferido(a)",
        RFDeixouDeFrequentar => "Deixou de frequentar",
        _ => "Pendente"
    };

    public const decimal MediaAprovacao = 5.0m;
}
