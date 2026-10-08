using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Application.Atas;

// Regras da Ata de Resultados Finais, sem acesso a banco (para poderem ser testadas isoladamente).
public static class AtaCalculo
{
    public sealed record ResultadoAluno(IReadOnlyList<AtaAlunoDisciplinaDto> Disciplinas, string RF, string? Pendencia);

    // Resultado automático de um aluno ativo, a partir do resultado anual de cada disciplina (o mesmo da tela de
    // Resultados, que já considera a Avaliação Final pela regra "vale a maior"):
    // - Aprovado: todas as disciplinas com ano completo e resultado >= 5,0;
    // - Conservado: ano completo e alguma disciplina abaixo de 5,0 mesmo com a Avaliação Final lançada;
    // - Pendente: falta nota em alguma disciplina, ou alguma está abaixo de 5,0 sem Avaliação Final lançada.
    // Disciplina da série sem nenhum lançamento na turma inteira (ex.: não ofertada) não trava o resultado e
    // aparece como "—"; se a turma tem lançamentos nela e o aluno não, o aluno fica Pendente.
    public static ResultadoAluno Calcular(
        IReadOnlyList<AtaDisciplinaColunaDto> disciplinas,
        IReadOnlyList<ResultadoAcademicoDto> resultadosDoAluno,
        IReadOnlySet<Guid> disciplinasComLancamentoNaTurma)
    {
        var porDisciplina = resultadosDoAluno
            .GroupBy(r => r.DisciplinaId)
            .ToDictionary(g => g.Key, g => g.First());

        var notas = new List<AtaAlunoDisciplinaDto>();
        var notasIncompletas = new List<string>();
        var semAvaliacaoFinal = new List<string>();
        var conservado = false;

        foreach (var disciplina in disciplinas)
        {
            if (!porDisciplina.TryGetValue(disciplina.DisciplinaId, out var resultado))
            {
                if (disciplinasComLancamentoNaTurma.Contains(disciplina.DisciplinaId))
                {
                    notasIncompletas.Add(disciplina.Nome);
                }

                continue;
            }

            notas.Add(new AtaAlunoDisciplinaDto(disciplina.DisciplinaId, disciplina.Nome, disciplina.Codigo, resultado.ResultadoFinalAno));

            switch (resultado.Situacao)
            {
                case "Aprovado":
                    break;
                case "Reprovado" when resultado.RecuperacaoFinal.HasValue:
                    conservado = true;
                    break;
                case "Reprovado":
                    semAvaliacaoFinal.Add(disciplina.Nome);
                    break;
                default:
                    notasIncompletas.Add(disciplina.Nome);
                    break;
            }
        }

        var pendencias = new List<string>();
        if (notasIncompletas.Count > 0)
        {
            pendencias.Add($"Notas incompletas: {string.Join(", ", notasIncompletas)}");
        }

        if (semAvaliacaoFinal.Count > 0)
        {
            pendencias.Add($"Falta a Avaliação Final (em Resultados): {string.Join(", ", semAvaliacaoFinal)}");
        }

        if (notas.Count == 0 && pendencias.Count == 0)
        {
            pendencias.Add("Nenhuma nota lançada no ano.");
        }

        if (pendencias.Count > 0)
        {
            return new ResultadoAluno(notas, AtaConstantes.RFPendente, string.Join(". ", pendencias) + ".");
        }

        return new ResultadoAluno(notas, conservado ? AtaConstantes.RFConservado : AtaConstantes.RFAprovado, null);
    }

    // "Apto a Cursar": aprovado vai para a próxima série (pela Ordem do cadastro de Séries); conservado,
    // transferido ou que deixou de frequentar fica na mesma série. Sem próxima série cadastrada (ex.: 9º Ano)
    // ou com resultado pendente, fica em branco.
    public static string? AptoACursar(string rf, Guid serieAtualId, IReadOnlyList<SerieListItemDto> series)
    {
        var serieAtual = series.FirstOrDefault(s => s.Id == serieAtualId);
        if (serieAtual is null)
        {
            return null;
        }

        return rf switch
        {
            AtaConstantes.RFAprovado => series
                .Where(s => s.IsAtiva && s.Ordem > serieAtual.Ordem)
                .OrderBy(s => s.Ordem)
                .Select(s => s.Nome)
                .FirstOrDefault(),
            AtaConstantes.RFConservado or AtaConstantes.RFTransferido or AtaConstantes.RFDeixouDeFrequentar => serieAtual.Nome,
            _ => null
        };
    }
}
