namespace SistemaEscolar.Application.Atas;

public static class AtaConstantes
{
    public const string StatusRascunho = "Rascunho";
    public const string StatusFinalizada = "Finalizada";

    // Resultado de cada aluno na Ata (coluna "Resultado"). Pendente/Aprovado/Conservado são calculados pelo
    // sistema para alunos ativos; aluno inativo entra como Transferido e pode ser trocado para Deixou de frequentar.
    public const string RFPendente = "Pendente";
    public const string RFAprovado = "Aprovado";
    public const string RFConservado = "Conservado";
    public const string RFTransferido = "Transferido";
    public const string RFDeixouDeFrequentar = "DeixouDeFrequentar";

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
