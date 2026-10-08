using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Dashboard;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;
using SistemaEscolar.Tests.Support;
using Xunit;

namespace SistemaEscolar.Tests;

// Cenário: ano letivo 2026, 3 turmas do 6º ano com 2 alunos cada (A, B e C), 1 disciplina, 3 trimestres.
// Lançamentos só no 1º trimestre: turma A com 2 aprovados, turma B com 1 aprovado e 1 reprovado, turma C
// sem nada. Existe ainda uma turma de 2025, que não deve aparecer nos números de 2026.
public sealed class DashboardServiceTests
{
    private const int Ano = 2026;

    private static readonly Guid SerieId = Guid.NewGuid();
    private static readonly DisciplinaListItemDto Matematica =
        new(Guid.NewGuid(), "Matemática", "MAT", new[] { new DisciplinaSerieDto(SerieId, "6º Ano") }, 160, true);

    private static readonly TurmaListItemDto TurmaA = Turma("6º ANO A", Ano);
    private static readonly TurmaListItemDto TurmaB = Turma("6º ANO B", Ano);
    private static readonly TurmaListItemDto TurmaC = Turma("6º ANO C", Ano);
    private static readonly TurmaListItemDto TurmaAnoPassado = Turma("6º ANO A", Ano - 1);

    private static readonly AlunoListItemDto[] Alunos =
    {
        Aluno(TurmaA), Aluno(TurmaA),
        Aluno(TurmaB), Aluno(TurmaB),
        Aluno(TurmaC), Aluno(TurmaC),
        Aluno(TurmaAnoPassado),
    };

    private static readonly PeriodoListItemDto[] Periodos =
    {
        // Marcado como aberto, mas a janela já passou: não está disponível de fato.
        Periodo(1, HorarioBrasilia.Hoje.AddDays(-60), HorarioBrasilia.Hoje.AddDays(-30)),
        Periodo(2, HorarioBrasilia.Hoje.AddDays(-1), HorarioBrasilia.Hoje.AddDays(1)),
        Periodo(3, HorarioBrasilia.Hoje.AddDays(30), HorarioBrasilia.Hoje.AddDays(60)),
    };

    private static readonly NotaListItemDto[] Notas =
    {
        Nota(Alunos[0], 8m),
        Nota(Alunos[1], 7m),
        Nota(Alunos[2], 3m),
        Nota(Alunos[3], 6m),
    };

    [Fact]
    public async Task Kpis_SemFiltro_UsamOsLancamentosEAsDatasDosPeriodos()
    {
        var dados = await CriarServico().ObterDadosAsync(new DashboardFiltroDto(Ano, null, null, null, null));

        Assert.Equal(6, dados.Kpis.TotalAlunos);
        // 3 aprovados em 4 lançamentos (antes: resultado final do ano, ~0% enquanto faltam trimestres).
        Assert.Equal(75m, dados.Kpis.PercentualAprovacaoGeral);
        // 3 turmas x 2 alunos x 3 trimestres = 18 esperados, 4 lançados (antes: notas não finalizadas = 4).
        Assert.Equal(14, dados.Kpis.PendenciasLancamento);
        Assert.Equal(dados.Donut.Pendentes, dados.Kpis.PendenciasLancamento);
        // Só o 2º trimestre está dentro da janela de datas.
        Assert.Equal(1, dados.Kpis.PeriodosAbertos);
        Assert.Equal(3, dados.Kpis.TotalPeriodos);
    }

    [Fact]
    public async Task Kpis_RespeitamFiltroDeTrimestre()
    {
        var dados = await CriarServico().ObterDadosAsync(new DashboardFiltroDto(Ano, 1, null, null, null));

        Assert.Equal(2, dados.Kpis.PendenciasLancamento);
        Assert.Equal(0, dados.Kpis.PeriodosAbertos);
        Assert.Equal(1, dados.Kpis.TotalPeriodos);
    }

    [Fact]
    public async Task Kpis_RespeitamFiltroDeTurma()
    {
        var dados = await CriarServico().ObterDadosAsync(new DashboardFiltroDto(Ano, null, null, TurmaB.Id, null));

        Assert.Equal(2, dados.Kpis.TotalAlunos);
        Assert.Equal(50m, dados.Kpis.PercentualAprovacaoGeral);
        Assert.Equal(4, dados.Kpis.PendenciasLancamento);
    }

    [Fact]
    public async Task Kpis_RespeitamFiltroDeProfessor()
    {
        var professor = new ProfessorListItemDto(
            Guid.NewGuid(), "Professora da turma C", "c@escola", "00000000000",
            new[] { new ProfessorAtribuicaoDto(TurmaC.Id, TurmaC.Nome, "6º Ano", Matematica.Id, Matematica.Nome) },
            true);

        var dados = await CriarServico(professor).ObterDadosAsync(new DashboardFiltroDto(Ano, null, professor.Id, null, null));

        Assert.Equal(2, dados.Kpis.TotalAlunos);
        Assert.Equal(6, dados.Kpis.PendenciasLancamento);
    }

    [Fact]
    public async Task TurmasDeOutroAno_NaoAparecemNoMapaDePendencias()
    {
        var dados = await CriarServico().ObterDadosAsync(new DashboardFiltroDto(Ano, null, null, null, null));

        Assert.Equal(3, dados.HeatmapCelulas.Count);
        Assert.DoesNotContain(dados.HeatmapCelulas, c => c.Esperadas == 0);
    }

    [Fact]
    public async Task Ranking_NaoRepeteTurmaNasDuasColunas()
    {
        var dados = await CriarServico().ObterDadosAsync(new DashboardFiltroDto(Ano, null, null, null, null));

        Assert.Equal(new[] { TurmaA.Nome }, dados.RankingMelhores.Select(x => x.Turma));
        Assert.Equal(new[] { TurmaB.Nome }, dados.RankingAtencao.Select(x => x.Turma));
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(3, 2, 1)]
    [InlineData(10, 5, 5)]
    [InlineData(12, 5, 5)]
    public async Task Ranking_DivideAsTurmasSemSobreposicao(int quantidadeTurmas, int melhoresEsperados, int atencaoEsperados)
    {
        var turmas = Enumerable.Range(1, quantidadeTurmas).Select(i => Turma($"Turma {i:00}", Ano)).ToList();
        var alunos = turmas.Select(Aluno).ToList();
        // Turma 01 com nota 10, turma 02 com 9 e assim por diante: aprovação vai caindo de turma em turma.
        var notas = alunos.SelectMany((aluno, i) => new[] { Nota(aluno, 9m), Nota(aluno, i < 9 ? 1m : 9m) }).ToList();
        var servico = new DashboardService(
            new AlunoServiceStub(alunos),
            new TurmaServiceStub(turmas),
            new DisciplinaServiceStub(new[] { Matematica }),
            new ProfessorServiceStub(),
            new PeriodoServiceStub(Periodos),
            new NotaServiceStub(notas));

        var dados = await servico.ObterDadosAsync(new DashboardFiltroDto(Ano, null, null, null, null));

        Assert.Equal(melhoresEsperados, dados.RankingMelhores.Count);
        Assert.Equal(atencaoEsperados, dados.RankingAtencao.Count);
        Assert.Empty(dados.RankingMelhores.Select(x => x.Turma).Intersect(dados.RankingAtencao.Select(x => x.Turma)));
    }

    private static DashboardService CriarServico(params ProfessorListItemDto[] professores) =>
        new(
            new AlunoServiceStub(Alunos),
            new TurmaServiceStub(new[] { TurmaA, TurmaB, TurmaC, TurmaAnoPassado }),
            new DisciplinaServiceStub(new[] { Matematica }),
            new ProfessorServiceStub(professores),
            new PeriodoServiceStub(Periodos),
            new NotaServiceStub(Notas));

    private static TurmaListItemDto Turma(string nome, int ano) =>
        new(Guid.NewGuid(), nome, SerieId, "6º Ano", "Matutino", ano, true);

    private static AlunoListItemDto Aluno(TurmaListItemDto turma) =>
        new(Guid.NewGuid(), "M", null, "00000000000", "Aluno", new DateTime(2014, 1, 1), turma.AnoLetivo, SerieId, "6º Ano", turma.Id, turma.Nome, true);

    private static PeriodoListItemDto Periodo(int trimestre, DateTime inicio, DateTime fim) =>
        new(Guid.NewGuid(), Ano, trimestre, $"{trimestre}º Trimestre", inicio, fim, IsAberto: true, AbertoManualmente: false);

    private static NotaListItemDto Nota(AlunoListItemDto aluno, decimal resultado) =>
        new(Guid.NewGuid(), aluno.Id, aluno.NomeCompleto, Matematica.Id, Matematica.Nome, Guid.NewGuid(), "Professor",
            Periodos[0].Id, Periodos[0].Descricao, Ano, 1, resultado, null, null, null, resultado, resultado, false);
}
