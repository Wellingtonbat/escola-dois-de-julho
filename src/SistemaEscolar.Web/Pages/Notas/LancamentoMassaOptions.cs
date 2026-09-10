using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Web.Pages.Notas;

// Monta os pares Turma+Disciplina disponíveis para o modal de Lançamento em Massa,
// respeitando o mesmo escopo do professor usado no resto da tela (só as turmas e
// disciplinas que ele leciona); Diretor/Coordenador/Secretária enxergam todas as
// combinações válidas (turma ativa cuja série tem a disciplina vinculada). Compartilhado
// entre Notas/Index e Notas/LancamentoMassa para que o modal de seleção seja idêntico
// nas duas telas.
public static class LancamentoMassaOptions
{
    public static async Task<(IReadOnlyList<SelectListItem> Turmas, string TurmaDisciplinaJson)> BuildAsync(
        IProfessorService professorService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService,
        ProfessorEscopoDto? escopo,
        CancellationToken cancellationToken)
    {
        if (escopo is not null)
        {
            var professores = await professorService.ListarAsync(null, cancellationToken);
            var professorAtual = professores.FirstOrDefault(p => p.Id == escopo.ProfessorId);
            var atribuicoes = professorAtual?.Atribuicoes ?? Array.Empty<ProfessorAtribuicaoDto>();

            var turmasEscopo = atribuicoes
                .Select(a => new { a.TurmaId, Label = $"{a.TurmaNome} ({a.SerieNome})" })
                .DistinctBy(x => x.TurmaId)
                .OrderBy(x => x.Label)
                .Select(x => new SelectListItem(x.Label, x.TurmaId.ToString()))
                .ToList();

            var jsonEscopo = JsonSerializer.Serialize(atribuicoes
                .Select(a => new { turmaId = a.TurmaId, disciplinaId = a.DisciplinaId, disciplinaNome = a.DisciplinaNome })
                .DistinctBy(x => (x.turmaId, x.disciplinaId)));

            return (turmasEscopo, jsonEscopo);
        }

        var turmas = await turmaService.ListarAsync(new TurmaListFilter(null, null, null, true), cancellationToken);
        var disciplinas = (await disciplinaService.ListarAsync(null, cancellationToken)).Where(d => d.IsAtiva).ToList();

        var turmasTodas = turmas
            .OrderBy(t => t.Nome)
            .Select(t => new SelectListItem($"{t.Nome} ({t.SerieNome})", t.Id.ToString()))
            .ToList();

        var pares = turmas.SelectMany(t => disciplinas
            .Where(d => d.Series.Any(s => s.SerieId == t.SerieId))
            .Select(d => new { turmaId = t.Id, disciplinaId = d.Id, disciplinaNome = d.Nome }));

        var jsonTodas = JsonSerializer.Serialize(pares);

        return (turmasTodas, jsonTodas);
    }
}
