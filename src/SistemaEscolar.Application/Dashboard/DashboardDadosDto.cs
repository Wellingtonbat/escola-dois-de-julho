namespace SistemaEscolar.Application.Dashboard;

public sealed record DashboardKpisDto(
    int TotalAlunos,
    decimal PercentualAprovacaoGeral,
    int PendenciasLancamento,
    int PeriodosAbertos,
    int TotalPeriodos);

public sealed record DashboardDonutDto(
    int Aprovados,
    int Reprovados,
    int Pendentes,
    int Total);

public sealed record DashboardBarraDisciplinaDto(
    string Disciplina,
    decimal Media);

public sealed record DashboardEvolucaoTrimestreDto(
    int Trimestre,
    decimal? Media);

public sealed record DashboardRankingTurmaDto(
    string Turma,
    decimal PercentualAprovacao,
    int TotalAlunos);

public sealed record DashboardHeatmapCelulaDto(
    string Turma,
    string Disciplina,
    int Lancadas,
    int Esperadas);

public sealed record DashboardDadosDto(
    int AnoLetivo,
    int? Trimestre,
    string? TurmaSelecionadaNome,
    string? DisciplinaSelecionadaNome,
    string? ProfessorSelecionadoNome,
    DashboardKpisDto Kpis,
    DashboardDonutDto Donut,
    IReadOnlyList<DashboardBarraDisciplinaDto> MediasPorDisciplina,
    IReadOnlyList<DashboardEvolucaoTrimestreDto> EvolucaoTrimestres,
    IReadOnlyList<DashboardRankingTurmaDto> RankingMelhores,
    IReadOnlyList<DashboardRankingTurmaDto> RankingAtencao,
    IReadOnlyList<string> HeatmapTurmas,
    IReadOnlyList<string> HeatmapDisciplinas,
    IReadOnlyList<DashboardHeatmapCelulaDto> HeatmapCelulas);
