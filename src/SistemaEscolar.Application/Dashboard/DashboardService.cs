using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private const decimal MediaAprovacao = 5.0m;
    private const int TamanhoRanking = 5;

    private readonly IAlunoService _alunoService;
    private readonly ITurmaService _turmaService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IProfessorService _professorService;
    private readonly IPeriodoService _periodoService;
    private readonly INotaRepository _notaRepository;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(
        IAlunoService alunoService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService,
        IProfessorService professorService,
        IPeriodoService periodoService,
        INotaRepository notaRepository,
        ICurrentUserService currentUserService)
    {
        _alunoService = alunoService;
        _turmaService = turmaService;
        _disciplinaService = disciplinaService;
        _professorService = professorService;
        _periodoService = periodoService;
        _notaRepository = notaRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardDadosDto> ObterDadosAsync(DashboardFiltroDto filtro, CancellationToken cancellationToken = default)
    {
        filtro = await AplicarEscopoDoProfessorAsync(filtro, cancellationToken);

        var alunos = await _alunoService.ListarAsync(null, cancellationToken);
        // Só as turmas do ano letivo filtrado: turmas de anos anteriores não têm alunos nem lançamentos neste
        // ano e apareciam no Mapa de Pendências (0 de 0) e nos demais cálculos por turma.
        var turmas = (await _turmaService.ListarAsync(new TurmaListFilter(null, null, filtro.AnoLetivo, true), cancellationToken))
            .Where(t => t.AnoLetivo == filtro.AnoLetivo)
            .ToList();
        var disciplinas = (await _disciplinaService.ListarAsync(null, cancellationToken)).Where(d => d.IsAtiva).ToList();
        var professores = (await _professorService.ListarAsync(null, cancellationToken)).Where(p => p.IsAtivo).ToList();
        var periodosAno = (await _periodoService.ListarAsync(null, cancellationToken))
            .Where(p => p.AnoLetivo == filtro.AnoLetivo)
            .ToList();
        // Direto do repositório: o INotaService restringe a lista aos lançamentos do professor logado, o que
        // impediria a visão "Escola inteira" dele. O recorte por professor é feito aqui, pelo filtro.
        var notasAno = await _notaRepository.GetAllAsync(new NotaListFilter(null, filtro.AnoLetivo, null, null), cancellationToken);

        var turmaSelecionada = filtro.TurmaId.HasValue ? turmas.FirstOrDefault(t => t.Id == filtro.TurmaId.Value) : null;
        var disciplinaSelecionada = filtro.DisciplinaId.HasValue ? disciplinas.FirstOrDefault(d => d.Id == filtro.DisciplinaId.Value) : null;
        // Professor informado mas não encontrado entre os ativos (ex.: professor logado sem cadastro vinculado):
        // recorte vazio, em vez de cair no "sem filtro" e mostrar a escola inteira.
        var professorSelecionado = filtro.ProfessorId.HasValue
            ? professores.FirstOrDefault(p => p.Id == filtro.ProfessorId.Value)
                ?? new ProfessorListItemDto(filtro.ProfessorId.Value, string.Empty, string.Empty, string.Empty, Array.Empty<ProfessorAtribuicaoDto>(), false)
            : null;

        var donut = CalcularDonut(filtro, alunos, turmas, disciplinas, periodosAno, notasAno, turmaSelecionada, disciplinaSelecionada, professorSelecionado);
        var kpis = CalcularKpis(filtro, alunos, turmas, periodosAno, donut, turmaSelecionada, professorSelecionado);
        var mediasPorDisciplina = CalcularMediasPorDisciplina(filtro, alunos, disciplinas, notasAno, turmaSelecionada, professorSelecionado);
        var evolucao = CalcularEvolucaoTrimestres(alunos, notasAno, turmaSelecionada, disciplinaSelecionada, professorSelecionado);
        var (rankingMelhores, rankingAtencao) = CalcularRankingTurmas(filtro, alunos, turmas, notasAno, disciplinaSelecionada, professorSelecionado);
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

    // Professor (sem perfil de gestão) escolhe entre "Minhas turmas" (o próprio ProfessorId) e "Escola inteira"
    // (ProfessorId nulo), que só mostra números agregados. Qualquer outro professor informado no filtro é
    // trocado pelo dele, para que ele não consiga ver o recorte individual de um colega.
    private async Task<DashboardFiltroDto> AplicarEscopoDoProfessorAsync(DashboardFiltroDto filtro, CancellationToken cancellationToken)
    {
        if (filtro.ProfessorId is null || !PermissoesPerfil.EhApenasProfessor(_currentUserService.IsInRole))
        {
            return filtro;
        }

        var escopo = await _professorService.ObterEscopoPorUsuarioAsync(_currentUserService.UserName, cancellationToken);
        return filtro with { ProfessorId = escopo?.ProfessorId ?? Guid.Empty };
    }

    // Cards do topo: seguem o mesmo recorte dos gráficos (professor, turma, disciplina e trimestre). Antes,
    // "Aprovação geral" usava o resultado final do ano (Pendente até os 3 trimestres serem lançados, então
    // ficava perto de 0% no meio do ano), "Pendências" contava notas não finalizadas (botão quase nunca usado,
    // e por isso não batia com os Pendentes do gráfico de rosca) e "Períodos em aberto" ignorava a janela de
    // datas. Agora aprovação e pendências vêm da mesma conta do gráfico de rosca, e os períodos usam a
    // disponibilidade efetiva (data + exceção manual).
    private static DashboardKpisDto CalcularKpis(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<TurmaListItemDto> turmasDoAno,
        IReadOnlyList<PeriodoListItemDto> periodosAno,
        DashboardDonutDto donut,
        TurmaListItemDto? turmaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        var alunosDoAno = alunos.Where(a => a.IsAtivo && a.AnoLetivo == filtro.AnoLetivo);
        if (turmaSelecionada is not null || professorSelecionado is not null)
        {
            var turmasNoRecorte = turmasDoAno
                .Where(t => turmaSelecionada is null || t.Id == turmaSelecionada.Id)
                .Where(t => professorSelecionado is null || professorSelecionado.Atribuicoes.Any(a => a.TurmaId == t.Id))
                .Select(t => t.Id)
                .ToHashSet();
            alunosDoAno = alunosDoAno.Where(a => a.TurmaId.HasValue && turmasNoRecorte.Contains(a.TurmaId.Value));
        }

        var lancados = donut.Aprovados + donut.Reprovados;
        var percentualAprovacao = lancados > 0 ? Math.Round(100m * donut.Aprovados / lancados, 1) : 0m;

        var periodosConsiderados = filtro.Trimestre.HasValue
            ? periodosAno.Where(p => p.Trimestre == filtro.Trimestre.Value).ToList()
            : periodosAno;
        var agora = HorarioBrasilia.Agora;
        var periodosAbertos = periodosConsiderados.Count(p => PeriodoDisponibilidade.EstaAberto(p, agora));

        return new DashboardKpisDto(alunosDoAno.Count(), percentualAprovacao, donut.Pendentes, periodosAbertos, periodosConsiderados.Count);
    }

    // "Aprovados x Reprovados x Pendentes": mesmo problema do ranking de turmas (ver CalcularRankingTurmas) —
    // contava pelo resultado final do ano, que fica "Pendente" até o aluno ter os 3 trimestres lançados, e
    // nunca levava o filtro de trimestre em conta mesmo a legenda do card anunciando o trimestre escolhido.
    // Passa a contar os lançamentos de nota do período filtrado: aprovado/reprovado = lançamento feito, acima
    // ou abaixo de 5,0; pendente = lançamento que ainda falta fazer. "Falta fazer" não é o campo IsFinalizada
    // (na prática quase nenhuma escola usa o botão de finalizar — isso deixaria o card sempre 100% pendente),
    // e sim a mesma comparação "esperado x lançado" já usada no Mapa de Pendências (CalcularHeatmap): quantos
    // lançamentos de aluno+disciplina+trimestre deveriam existir no recorte filtrado versus quantos existem.
    private static DashboardDonutDto CalcularDonut(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<TurmaListItemDto> turmas,
        IReadOnlyList<DisciplinaListItemDto> disciplinas,
        IReadOnlyList<PeriodoListItemDto> periodosAno,
        IReadOnlyList<NotaListItemDto> notasAno,
        TurmaListItemDto? turmaSelecionada,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        var (esperados, lancamentos) = CalcularEsperadosELancamentos(
            filtro, alunos, turmas, disciplinas, periodosAno, notasAno, turmaSelecionada, disciplinaSelecionada, professorSelecionado);

        var aprovados = lancamentos.Count(n => n.ResultadoFinalUnidade >= MediaAprovacao);
        var reprovados = lancamentos.Count - aprovados;
        var pendentes = Math.Max(0, esperados - lancamentos.Count);

        return new DashboardDonutDto(aprovados, reprovados, pendentes, aprovados + reprovados + pendentes);
    }

    // Quantos lançamentos de aluno+disciplina+trimestre são esperados no recorte filtrado (turma, disciplina,
    // professor, trimestre) e quais já existem. Mesma conta do Mapa de Pendências (CalcularHeatmap), só que
    // somada em vez de célula por célula, e já considerando a turma/disciplina selecionadas no filtro.
    private static (int Esperados, IReadOnlyList<NotaListItemDto> Lancamentos) CalcularEsperadosELancamentos(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<TurmaListItemDto> turmas,
        IReadOnlyList<DisciplinaListItemDto> disciplinas,
        IReadOnlyList<PeriodoListItemDto> periodosAno,
        IReadOnlyList<NotaListItemDto> notasAno,
        TurmaListItemDto? turmaSelecionada,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        var lancamentos = AplicarFiltrosNota(filtro, alunos, notasAno, turmaSelecionada, disciplinaSelecionada, professorSelecionado).ToList();

        var turmasConsideradas = turmaSelecionada is not null ? new[] { turmaSelecionada } : turmas;
        var disciplinasConsideradas = disciplinaSelecionada is not null ? new[] { disciplinaSelecionada } : disciplinas;
        var periodosConsiderados = filtro.Trimestre.HasValue
            ? periodosAno.Count(p => p.Trimestre == filtro.Trimestre.Value)
            : periodosAno.Count;

        var alunosPorTurma = alunos
            .Where(a => a.IsAtivo && a.AnoLetivo == filtro.AnoLetivo && a.TurmaId.HasValue)
            .GroupBy(a => a.TurmaId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var esperados = 0;
        foreach (var turma in turmasConsideradas)
        {
            var qtdAlunos = alunosPorTurma.TryGetValue(turma.Id, out var qtd) ? qtd : 0;
            foreach (var disciplina in disciplinasConsideradas)
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

                esperados += qtdAlunos * periodosConsiderados;
            }
        }

        return (esperados, lancamentos);
    }

    // "Média por disciplina": mesmo ajuste de origem dos dados que os cards anteriores (usa os lançamentos do
    // período filtrado, não o resultado final do ano). Além disso, antes só listava a disciplina que já tinha
    // alguma nota lançada no recorte — uma disciplina da turma sem lançamento ainda simplesmente sumia do
    // gráfico. Agora lista todas as disciplinas da série da turma (a mesma checagem de vínculo do Mapa de
    // Pendências), com Media nula para quem ainda não tem lançamento nesse período.
    private static IReadOnlyList<DashboardBarraDisciplinaDto> CalcularMediasPorDisciplina(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<DisciplinaListItemDto> disciplinas,
        IReadOnlyList<NotaListItemDto> notasAno,
        TurmaListItemDto? turmaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        if (turmaSelecionada is null)
        {
            return Array.Empty<DashboardBarraDisciplinaDto>();
        }

        var baseParaBarras = AplicarFiltrosNota(filtro, alunos, notasAno, turmaSelecionada, null, professorSelecionado)
            .ToLookup(n => n.DisciplinaId);

        var disciplinasDaSerie = disciplinas.Where(d => d.Series.Any(s => s.SerieId == turmaSelecionada.SerieId));
        if (professorSelecionado is not null)
        {
            disciplinasDaSerie = disciplinasDaSerie.Where(d =>
                professorSelecionado.Atribuicoes.Any(a => a.TurmaId == turmaSelecionada.Id && a.DisciplinaId == d.Id));
        }

        return disciplinasDaSerie
            .Select(d =>
            {
                var notasDaDisciplina = baseParaBarras[d.Id];
                var media = notasDaDisciplina.Any() ? Math.Round(notasDaDisciplina.Average(x => x.ResultadoFinalUnidade), 1) : (decimal?)null;
                return new DashboardBarraDisciplinaDto(d.Nome, media);
            })
            .OrderByDescending(x => x.Media.HasValue)
            .ThenByDescending(x => x.Media)
            .ToList();
    }

    // Filtros compartilhados pelos cards que mostram o período selecionado (turma, disciplina, professor e
    // trimestre) a partir dos lançamentos de nota do ano. O vínculo com a turma passa pelo aluno, porque
    // NotaListItemDto não guarda o TurmaId diretamente.
    private static IEnumerable<NotaListItemDto> AplicarFiltrosNota(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<NotaListItemDto> notasAno,
        TurmaListItemDto? turmaSelecionada,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        IEnumerable<NotaListItemDto> filtradas = filtro.Trimestre.HasValue
            ? notasAno.Where(n => n.Trimestre == filtro.Trimestre.Value)
            : notasAno;

        if (turmaSelecionada is not null)
        {
            var alunosDaTurma = alunos.Where(a => a.TurmaId == turmaSelecionada.Id).Select(a => a.Id).ToHashSet();
            filtradas = filtradas.Where(n => alunosDaTurma.Contains(n.AlunoId));
        }

        if (disciplinaSelecionada is not null)
        {
            filtradas = filtradas.Where(n => n.DisciplinaId == disciplinaSelecionada.Id);
        }

        if (professorSelecionado is not null)
        {
            filtradas = filtradas.Where(n => n.ProfessorId == professorSelecionado.Id);
        }

        return filtradas;
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

    // Ranking de turmas por desempenho atual. Usa os lançamentos de nota (por trimestre), e não o resultado
    // final do ano (ResultadoAcademicoDto): esse é "Pendente" até o aluno ter os 3 trimestres lançados, então
    // no meio do ano quase ninguém está "Aprovado" ainda e o ranking apareceria com 0% para todas as turmas.
    // Usar os lançamentos também é o que permite este card respeitar o filtro de trimestre, como o rótulo já indica.
    private static (IReadOnlyList<DashboardRankingTurmaDto> Melhores, IReadOnlyList<DashboardRankingTurmaDto> Atencao) CalcularRankingTurmas(
        DashboardFiltroDto filtro,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<TurmaListItemDto> turmas,
        IReadOnlyList<NotaListItemDto> notasAno,
        DisciplinaListItemDto? disciplinaSelecionada,
        ProfessorListItemDto? professorSelecionado)
    {
        IEnumerable<NotaListItemDto> baseParaRanking = filtro.Trimestre.HasValue
            ? notasAno.Where(n => n.Trimestre == filtro.Trimestre.Value)
            : notasAno;

        if (disciplinaSelecionada is not null)
        {
            baseParaRanking = baseParaRanking.Where(n => n.DisciplinaId == disciplinaSelecionada.Id);
        }

        if (professorSelecionado is not null)
        {
            baseParaRanking = baseParaRanking.Where(n => n.ProfessorId == professorSelecionado.Id);
        }

        var turmaIdPorAluno = alunos.ToDictionary(a => a.Id, a => a.TurmaId);
        var nomeTurmaPorId = turmas.ToDictionary(t => t.Id, t => t.Nome);

        var porTurma = baseParaRanking
            .Where(n => turmaIdPorAluno.TryGetValue(n.AlunoId, out var turmaId) && turmaId.HasValue && nomeTurmaPorId.ContainsKey(turmaId.Value))
            .GroupBy(n => turmaIdPorAluno[n.AlunoId]!.Value)
            .Select(g => new DashboardRankingTurmaDto(
                nomeTurmaPorId[g.Key],
                Math.Round(100m * g.Count(x => x.ResultadoFinalUnidade >= MediaAprovacao) / g.Count(), 1),
                g.Select(x => x.AlunoId).Distinct().Count()))
            .OrderByDescending(x => x.PercentualAprovacao)
            .ToList();

        // As duas colunas nunca repetem turma: com poucas turmas (menos de 2 x TamanhoRanking), a metade de
        // cima vai para "Melhor desempenho" e o restante para "Atenção necessária", da pior para a melhor.
        var quantidadeMelhores = Math.Min(TamanhoRanking, (porTurma.Count + 1) / 2);
        var melhores = porTurma.Take(quantidadeMelhores).ToList();
        var atencao = porTurma
            .Skip(quantidadeMelhores)
            .Reverse()
            .Take(TamanhoRanking)
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
}
