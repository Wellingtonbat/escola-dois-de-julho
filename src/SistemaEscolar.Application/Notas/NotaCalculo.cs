namespace SistemaEscolar.Application.Notas;

public static class NotaCalculo
{
    public sealed record Resultado(
        bool Lancado,
        decimal? Soma,
        decimal Final,
        bool PodeRecuperar,
        string StatusLabel,
        string StatusClass);

    public static Resultado Calcular(NotaListItemDto? nota)
    {
        if (nota is null)
        {
            return new Resultado(false, null, 0m, false, "Não lançado", "status-pill status-neutral");
        }

        var avals = new[] { nota.Avaliacao1, nota.Avaliacao2, nota.Avaliacao3 }.Where(x => x.HasValue).ToList();
        var lancado = avals.Count > 0;
        var soma = lancado ? nota.ResultadoUnidade : (decimal?)null;
        var podeRecuperar = soma.HasValue && soma.Value < 5m;
        var final = nota.ResultadoFinalUnidade;

        string statusLabel;
        string statusClass;
        if (!lancado)
        {
            statusLabel = "Não lançado";
            statusClass = "status-pill status-neutral";
        }
        else if (avals.Count < 3)
        {
            statusLabel = "Em andamento";
            statusClass = "status-pill status-warn";
        }
        else
        {
            statusLabel = final >= 5m ? "Aprovado" : "Reprovado";
            statusClass = final >= 5m ? "status-pill status-open" : "status-pill status-danger";
        }

        return new Resultado(lancado, soma, final, podeRecuperar, statusLabel, statusClass);
    }
}
