using System.Globalization;
using SistemaEscolar.Application.Periodos;

namespace SistemaEscolar.Application.Notas;

// Cálculo compartilhado do boletim (PDF do aluno e planilha de Resultados usam exatamente a mesma regra):
// por trimestre, Res.Un = soma das 3 avaliações e Res.Fi = maior valor entre a soma e a recuperação
// paralela (já vem pronto em NotaListItemDto); no fim do ano, Tot.Pts = soma dos 3 Res.Fi, Méd.Curso =
// média dos trimestres já lançados, e a situação usa a Recuperação Final (MAX com trava < 5,0) apenas
// quando os 3 trimestres já estiverem lançados.
public static class BoletimCalculo
{
    private const decimal MediaAprovacao = 5.0m;
    private const int TrimestresEsperados = 3;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public sealed record TrimestreResultado(string Av1, string Av2, string Av3, string ResUnidade, string RecParalela, string ResFinal);

    public sealed record DisciplinaResultado(
        IReadOnlyList<TrimestreResultado> Trimestres,
        bool TemLancamento,
        decimal TotalPontos,
        decimal? MediaCurso,
        decimal? RecuperacaoFinal,
        string Situacao);

    public static DisciplinaResultado Calcular(
        IReadOnlyList<PeriodoListItemDto> periodosDoAno,
        IReadOnlyList<NotaListItemDto> notasDaDisciplina,
        decimal? recuperacaoFinal)
    {
        var porTrimestre = periodosDoAno
            .Select(periodo => notasDaDisciplina.FirstOrDefault(n => n.PeriodoLancamentoId == periodo.Id))
            .ToList();

        var trimestres = porTrimestre
            .Select(nota => new TrimestreResultado(
                FormatarOuTraco(nota?.Avaliacao1),
                FormatarOuTraco(nota?.Avaliacao2),
                FormatarOuTraco(nota?.Avaliacao3),
                FormatarNumero(nota?.ResultadoUnidade ?? 0m),
                FormatarOuTraco(nota?.RecuperacaoParalela),
                FormatarNumero(nota?.ResultadoFinalUnidade ?? 0m)))
            .ToList();

        var lancamentos = porTrimestre.Where(n => n is not null).Select(n => n!.ResultadoFinalUnidade).ToList();

        if (lancamentos.Count == 0)
        {
            return new DisciplinaResultado(trimestres, false, 0m, null, recuperacaoFinal, "—");
        }

        var completo = lancamentos.Count == TrimestresEsperados;
        var totalPontos = lancamentos.Sum();
        var media = Math.Round(lancamentos.Average(), 2);
        var resultadoConsiderado = media;
        if (completo && media < MediaAprovacao && recuperacaoFinal.HasValue)
        {
            resultadoConsiderado = Math.Max(media, recuperacaoFinal.Value);
        }

        var situacao = resultadoConsiderado >= MediaAprovacao ? "AP" : "RP";
        return new DisciplinaResultado(trimestres, true, totalPontos, media, recuperacaoFinal, situacao);
    }

    public static string FormatarOuTraco(decimal? valor) =>
        valor.HasValue ? valor.Value.ToString("0.0", PtBr) : "—";

    public static string FormatarNumero(decimal valor) =>
        valor.ToString("0.0", PtBr);
}
