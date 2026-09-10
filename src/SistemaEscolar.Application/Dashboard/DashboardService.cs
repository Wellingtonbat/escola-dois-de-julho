using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IAlunoService _alunoService;
    private readonly ITurmaService _turmaService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IProfessorService _professorService;
    private readonly IPeriodoService _periodoService;
    private readonly INotaService _notaService;
    private readonly IResultadoAcademicoService _resultadoAcademicoService;

    public DashboardService(
        IAlunoService alunoService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService,
        IProfessorService professorService,
        IPeriodoService periodoService,
        INotaService notaService,
        IResultadoAcademicoService resultadoAcademicoService)
    {
        _alunoService = alunoService;
        _turmaService = turmaService;
        _disciplinaService = disciplinaService;
        _professorService = professorService;
        _periodoService = periodoService;
        _notaService = notaService;
        _resultadoAcademicoService = resultadoAcademicoService;
    }

    public async Task<DashboardDadosDto> ObterDadosAsync(DashboardFiltroDto filtro, CancellationToken cancellationToken = default)
    {
        var alunos = await _alunoService.ListarAsync(null, cancellationToken);
        var turmas = (await _turmaService.ListarAsync(new TurmaListFilter(null, null, null, true), cancellationToken)).ToList();
        var disciplinas = (await _disciplinaService.ListarAsync(null, cancellationToken)).Where(d => d.IsAtiva).ToList();
        var professores = (await _professorService.ListarAsync(null, cancellationToken)).Where(p => p.IsAtivo).ToList();
        var periodosAno = (await _periodoService.ListarAsync(null, cancellationToken))
            .Where(p => p.AnoLetivo == filtro.AnoLetivo)
            .ToList();
        var resultados = await _resultadoAcademicoService.ListarAsync(
            new ResultadoAcademicoFilter(filtro.AnoLetivo, null, null, "todos"), cancellationToken);
        var notasAno = await _notaService.ListarAsync(new NotaListFilter(null, filtro.AnoLetivo, null, null), cancellationToken);

        var turmaSelecionada = filtro.TurmaId.HasValue ? turmas.FirstOrDefault(t => t.Id == filtro.TurmaId.Value) : null;
        var disciplinaSelecionada = filtro.DisciplinaId.HasValue ? disciplinas.FirstOrDefault(d => d.Id == filtro.DisciplinaId.Value) : null;
        var professorSelecionado = filtro.ProfessorId.HasValue ? professores.FirstOrDefault(p => p.Id == filtro.ProfessorId.Value) : null;

        var kpis = CalcularKpis(filtro, alunos, resultados, notasAno, periodosAno);
        var donut = CalcularDonut(resultados, turmaSelecionada, disciplinaSelecionada, professorSelecionado);
        var mediasPorDisciplina = CalcularMediasPorDisciplina(resultados, turmaSelecionada, professorSelecionado);
        var evolucao = CalcularEvolucaoTrimestres(alunos, notasAno, turmaSelecionada, disciplinaSelecionada, professorSelecionado);
        var (rankingMelhores, rankingAtencao) = CalcularRankingTurmas(resultados, disciplinaSelecionada, professorSelecionado);
        var (heatmapTurmas, heatmapDisciplinas, heatmapCelulas) = CalcularHeatmap(
            filtro, alunos, turmas, disciplinas, professorSelecionado, notasAno, periodosAno);

        return new DashboardDadosDto(
            filtro.AnoLetivo,
            filtro.Trimestre,
            turmaSelecionada?.Nome,
            disciplinaSelecionada?.Nome,
            professorSelecionado?.NomeCompleto,
            kpis,
            donut,
            mediasPorDisciplina,
            evolucao,
            rankingMelhores,
            rankingAtencao,
            heatmapTurmas,
            heatmapDisciplinas,
            heatmapCelulas);
    }

    private static DashboardKpisDto CalcularKpis(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<ResultadoAcademicoDto> resultados,
        IReadOnlyList<NotaListItemDto> notasAno,
        IReadOnlyList<PeriodoListItemDto> periodosAno)
    {
        var totalAlunos = alunos.Count(a => a.IsAtivo && a.AnoLetivo == filtro.AnoLetivo);
        var percentualAprovacaoGeral = resultados.Count > 0
            ? Math.Round(100m * resultados.Count(r => r.Situacao == "Aprovado") / resultados.Count, 1)
            : 0m;
        var pendenciasLancamento = notasAno.Count(n => !n.IsFinalizada);
        var periodosAbertos = periodosAno.Count(p => p.IsAberto);

        return new DashboardKpisDto(totalAlunos, percentualAprovacaoGeral, pendenciasLancamento, periodosAbertos, periodosAno.Count);
    }

    private static DashboardDonutDto CalcularDonut(
        IReadOnlyList<ResultadoAcademicoDto> resultados,
        TurmaListItemDto? turmaSelecionada,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        var filtrados = AplicarFiltrosResultado(resultados, turmaSelecionada, disciplinaSelecionada, professorSelecionado).ToList();

        return new DashboardDonutDto(
            filtrados.Count(r => r.Situacao == "Aprovado"),
            filtrados.Count(r => r.Situacao == "Reprovado"),
            filtrados.Count(r => r.Situacao == "Pendente"),
            filtrados.Count);
    }

    private static IReadOnlyList<DashboardBarraDisciplinaDto> CalcularMediasPorDisciplina(
        IReadOnlyList<ResultadoAcademicoDto> resultados,
        TurmaListItemDto? turmaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        if (turmaSelecionada is null)
        {
            return Array.Empty<DashboardBarraDisciplinaDto>();
        }

        IEnumerable<ResultadoAcademicoDto> baseParaBarras = resultados
            .Where(r => string.Equals(r.Turma, turmaSelecionada.Nome, StringComparison.OrdinalIgnoreCase));

        if (professorSelecionado is not null)
        {
            baseParaBarras = baseParaBarras.Where(r => string.Equals(r.ProfessorNome, professorSelecionado.NomeCompleto, StringComparison.OrdinalIgnoreCase));
        }

        return baseParaBarras
            .GroupBy(r => r.Disciplina)
            .Select(g => new DashboardBarraDisciplinaDto(g.Key, Math.Round(g.Average(x => x.MediaFinal), 1)))
            .OrderByDescending(x => x.Media)
            .ToList();
    }

    private static IReadOnlyList<DashboardEvolucaoTrimestreDto> CalcularEvolucaoTrimestres(
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<NotaListItemDto> notasAno,
        TurmaListItemDto? turmaSelecionada,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        IEnumerable<NotaListItemDto> baseParaEvolucao = notasAno;

        if (turmaSelecionada is not null)
        {
            var alunosDaTurma = alunos.Where(a => a.TurmaId == turmaSelecionada.Id).Select(a => a.Id).ToHashSet();
            baseParaEvolucao = baseParaEvolucao.Where(n => alunosDaTurma.Contains(n.AlunoId));
        }

        if (disciplinaSelecionada is not null)
        {
            baseParaEvolucao = baseParaEvolucao.Where(n => n.DisciplinaId == disciplinaSelecionada.Id);
        }

        if (professorSelecionado is not null)
        {
            baseParaEvolucao = baseParaEvolucao.Where(n => n.ProfessorId == professorSelecionado.Id);
        }

        var baseList = baseParaEvolucao.ToList();

        return new[] { 1, 2, 3 }.Select(trimestre =>
        {
            var doTrimestre = baseList.Where(n => n.Trimestre == trimestre).ToList();
            decimal? media = doTrimestre.Count > 0 ? Math.Round(doTrimestre.Average(n => n.ResultadoFinalUnidade), 1) : null;
            return new DashboardEvolucaoTrimestreDto(trimestre, media);
        }).ToList();
    }

    private static (IReadOnlyList<DashboardRankingTurmaDto> Melhores, IReadOnlyList<DashboardRankingTurmaDto> Atencao) CalcularRankingTurmas(
        IReadOnlyList<ResultadoAcademicoDto> resultados,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        IEnumerable<ResultadoAcademicoDto> baseParaRanking = resultados;

        if (disciplinaSelecionada is not null)
        {
            baseParaRanking = baseParaRanking.Where(r => r.DisciplinaId == disciplinaSelecionada.Id);
        }

        if (professorSelecionado is not null)
        {
            baseParaRanking = baseParaRanking.Where(r => string.Equals(r.ProfessorNome, professorSelecionado.NomeCompleto, StringComparison.OrdinalIgnoreCase));
        }

        var porTurma = baseParaRanking
            .GroupBy(r => r.Turma)
            .Select(g => new DashboardRankingTurmaDto(
                g.Key,
                Math.Round(100m * g.Count(x => x.Situacao == "Aprovado") / g.Count(), 1),
                g.Select(x => x.AlunoId).Distinct().Count()))
            .OrderByDescending(x => x.PercentualAprovacao)
            .ToList();

        var melhores = porTurma.Take(5).ToList();
        var atencao = porTurma
            .Skip(Math.Max(0, porTurma.Count - 5))
            .OrderBy(x => x.PercentualAprovacao)
            .ToList();

        return (melhores, atencao);
    }

    private static (IReadOnlyList<string> Turmas, IReadOnlyList<string> Disciplinas, IReadOnlyList<DashboardHeatmapCelulaDto> Celulas) CalcularHeatmap(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<TurmaListItemDto> turmas,
        IReadOnlyList<DisciplinaListItemDto> disciplinas,
        ProfessorListItemDto? professorSelecionado,
        IReadOnlyList<NotaListItemDto> notasAno,
        IReadOnlyList<PeriodoListItemDto> periodosAno)
    {
        var periodosConsiderados = filtro.Trimestre.HasValue
            ? periodosAno.Where(p => p.Trimestre == filtro.Trimestre.Value).ToList()
            : periodosAno;

        IEnumerable<NotaListItemDto> notasConsideradas = filtro.Trimestre.HasValue
            ? notasAno.Where(n => n.Trimestre == filtro.Trimestre.Value)
            : notasAno;

        if (professorSelecionado is not null)
        {
            notasConsideradas = notasConsideradas.Where(n => n.ProfessorId == professorSelecionado.Id);
        }

        var notasConsideradasList = notasConsideradas.ToList();
        var alunoTurmaMap = alunos.ToDictionary(a => a.Id, a => a.TurmaId);
        var alunosPorTurma = alunos
            .Where(a => a.IsAtivo && a.AnoLetivo == filtro.AnoLetivo && a.TurmaId.HasValue)
            .GroupBy(a => a.TurmaId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var pares = new List<(TurmaListItemDto Turma, DisciplinaListItemDto Disciplina)>();
        foreach (var turma in turmas)
        {
            foreach (var disciplina in disciplinas)
            {
                if (!disciplina.Series.Any(s => s.SerieId == turma.SerieId))
                {
                    continue;
                }

                if (professorSelecionado is not null
                    && !professorSelecionado.Atribuicoes.Any(a => a.TurmaId == turma.Id && a.DisciplinaId == disciplina.Id))
                {
                    continue;
                }

                pares.Add((turma, disciplina));
            }
        }

        var celulas = pares.Select(par =>
        {
            var alunosDaTurma = alunosPorTurma.TryGetValue(par.Turma.Id, out var qtd) ? qtd : 0;
            var esperadas = alunosDaTurma * periodosConsiderados.Count;
            var lancadas = notasConsideradasList.Count(n =>
                n.DisciplinaId == par.Disciplina.Id
                && alunoTurmaMap.TryGetValue(n.AlunoId, out var turmaId)
                && turmaId == par.Turma.Id);

            return new DashboardHeatmapCelulaDto(par.Turma.Nome, par.Disciplina.Nome, lancadas, esperadas);
        }).ToList();

        var heatmapTurmas = pares.Select(p => p.Turma.Nome).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var heatmapDisciplinas = pares.Select(p => p.Disciplina.Nome).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        return (heatmapTurmas, heatmapDisciplinas, celulas);
    }

    private static IEnumerable<ResultadoAcademicoDto> AplicarFiltrosResultado(
        IReadOnlyList<ResultadoAcademicoDto> resultados,
        TurmaListItemDto? turmaSelecionada,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        IEnumerable<ResultadoAcademicoDto> filtrados = resultados;

        if (turmaSelecionada is not null)
        {
            filtrados = filtrados.Where(r => string.Equals(r.Turma, turmaSelecionada.Nome, StringComparison.OrdinalIgnoreCase));
        }

        if (disciplinaSelecionada is not null)
        {
            filtrados = filtrados.Where(r => r.DisciplinaId == disciplinaSelecionada.Id);
        }

        if (professorSelecionado is not null)
        {
            filtrados = filtrados.Where(r => string.Equals(r.ProfessorNome, professorSelecionado.NomeCompleto, StringComparison.OrdinalIgnoreCase));
        }

        return filtrados;
    }
}
