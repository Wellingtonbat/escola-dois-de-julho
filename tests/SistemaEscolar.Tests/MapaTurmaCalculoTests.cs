using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Turmas;
using SistemaEscolar.Tests.Support;
using Xunit;

namespace SistemaEscolar.Tests;

public sealed class MapaTurmaCalculoTests
{
    private const int Ano = 2026;
    private static readonly Guid SerieId = Guid.NewGuid();
    private static readonly TurmaListItemDto Turma = new(Guid.NewGuid(), "6º ANO A", SerieId, "6º Ano", "Matutino", Ano, true);
    private static readonly DisciplinaListItemDto Matematica = Disciplina("Matemática", "MAT");
    private static readonly DisciplinaListItemDto Portugues = Disciplina("Português", "POR");

    private static readonly PeriodoListItemDto T1 = Periodo(1, new DateTime(Ano, 2, 1));
    private static readonly PeriodoListItemDto T2 = Periodo(2, new DateTime(Ano, 6, 1));
    private static readonly PeriodoListItemDto T3 = Periodo(3, new DateTime(Ano, 9, 1));

    // Meio do 2º trimestre: o 3º ainda não começou.
    private static readonly DateTime Agora = new(Ano, 7, 15, 10, 0, 0);

    private static readonly AlunoListItemDto Ana = Aluno("Ana", ativo: true);
    private static readonly AlunoListItemDto Bruno = Aluno("Bruno", ativo: true);
    private static readonly AlunoListItemDto Carla = Aluno("Carla", ativo: false);

    private static MapaTurmaDto Montar(params NotaListItemDto[] notas) =>
        MapaTurmaCalculo.Montar(Turma, new[] { T1, T2, T3 }, new[] { Bruno, Carla, Ana }, new[] { Portugues, Matematica }, notas, Agora);

    [Fact]
    public void NotaDoTrimestre_UsaOResultadoFinalDaUnidade_EMediaDosLancados()
    {
        var mapa = Montar(
            Nota(Ana, Matematica, T1, 2m, 2m, 2m, rec: null, final: 6m),
            Nota(Ana, Matematica, T2, 1m, 1m, 1m, rec: 4m, final: 4m));

        var ana = mapa.Alunos.Single(a => a.Nome == "Ana");
        var mat = ana.Disciplinas[mapa.Disciplinas.ToList().FindIndex(d => d.Codigo == "MAT")];

        Assert.Equal(6m, mat.Trimestres[0].Valor);
        Assert.Equal(4m, mat.Trimestres[1].Valor);
        Assert.True(mat.Trimestres[1].AbaixoDaMedia);
        Assert.Equal(MapaNotaEstado.NaoIniciado, mat.Trimestres[2].Estado);
        Assert.Equal(5m, mat.Media);
        Assert.False(mat.MediaAbaixo);
    }

    [Fact]
    public void SemLancamentoEmTrimestreIniciado_EPendencia_EEntraNaSituacao()
    {
        var mapa = Montar(Nota(Ana, Matematica, T1, 1m, 1m, 1m, rec: null, final: 3m));

        var ana = mapa.Alunos.Single(a => a.Nome == "Ana");
        // POR sem nota no 1º e 2º; MAT sem nota no 2º. O 3º trimestre não conta.
        Assert.Equal(3, ana.NotasPendentes);
        Assert.Equal(new[] { "MAT" }, ana.AbaixoDaMedia);
        Assert.Equal("Abaixo da média em 1: MAT; 3 notas não lançadas", ana.Situacao);
    }

    [Fact]
    public void LancamentoIncompleto_NaoEhMarcadoComoAbaixoDaMedia()
    {
        var mapa = Montar(Nota(Ana, Matematica, T1, 2m, null, null, rec: null, final: 2m));

        var mat = mapa.Alunos.Single(a => a.Nome == "Ana").Disciplinas[mapa.Disciplinas.ToList().FindIndex(d => d.Codigo == "MAT")];
        Assert.Equal(MapaNotaEstado.Incompleta, mat.Trimestres[0].Estado);
        Assert.False(mat.Trimestres[0].AbaixoDaMedia);
    }

    [Fact]
    public void LancamentoIncompleto_FicaForaDasMedias_EApareceNaSituacao()
    {
        var mapa = Montar(
            Nota(Ana, Matematica, T1, 3m, 3m, 2m, rec: null, final: 8m),
            Nota(Ana, Matematica, T2, 1m, null, null, rec: null, final: 1m),
            Nota(Bruno, Matematica, T2, 1m, null, null, rec: null, final: 1m));

        var indice = mapa.Disciplinas.ToList().FindIndex(d => d.Codigo == "MAT");
        var ana = mapa.Alunos.Single(a => a.Nome == "Ana");
        Assert.Equal(8m, ana.Disciplinas[indice].Media);
        Assert.Equal(1, ana.NotasIncompletas);
        Assert.Contains("1 nota incompleta", ana.Situacao);
        Assert.Null(mapa.Resumo[indice].MediaTurmaTrimestres[1]);
    }

    [Fact]
    public void AlunoInativo_ApareceComoTransferido_SemNotas_EForaDoResumo()
    {
        var mapa = Montar(
            Nota(Carla, Matematica, T1, 1m, 1m, 1m, rec: null, final: 3m),
            Nota(Ana, Matematica, T1, 3m, 3m, 3m, rec: null, final: 9m));

        var carla = mapa.Alunos.Single(a => a.Nome == "Carla");
        Assert.True(carla.Transferido);
        Assert.Equal("Transferido(a)", carla.Situacao);
        Assert.All(carla.Disciplinas, d => Assert.Null(d.Media));

        var resumoMat = mapa.Resumo[mapa.Disciplinas.ToList().FindIndex(d => d.Codigo == "MAT")];
        Assert.Equal(9m, resumoMat.MediaTurmaTrimestres[0]);
        Assert.Equal(1, resumoMat.SemNotaTrimestres[0]); // Bruno
    }

    [Fact]
    public void Alunos_EmOrdemAlfabetica_ComNumeracao_EDisciplinasPorNome()
    {
        var mapa = Montar();

        Assert.Equal(new[] { "Ana", "Bruno", "Carla" }, mapa.Alunos.Select(a => a.Nome));
        Assert.Equal(new[] { 1, 2, 3 }, mapa.Alunos.Select(a => a.Numero));
        Assert.Equal(new[] { "Matemática", "Português" }, mapa.Disciplinas.Select(d => d.Nome));
    }

    [Fact]
    public void VisaoRestrita_MostraSoAsDisciplinasPermitidas()
    {
        var mapa = MapaTurmaCalculo.Montar(Turma, new[] { T1, T2, T3 }, new[] { Ana }, new[] { Portugues, Matematica },
            Array.Empty<NotaListItemDto>(), Agora, new HashSet<Guid> { Matematica.Id });

        Assert.Equal(new[] { "MAT" }, mapa.Disciplinas.Select(d => d.Codigo));
    }

    [Theory]
    [InlineData(Perfis.Diretor, true)]
    [InlineData(Perfis.ViceDiretor, true)]
    [InlineData(Perfis.Coordenador, true)]
    [InlineData(Perfis.Secretaria, false)]
    [InlineData(Perfis.Professor, false)]
    public void PermissaoMapaDaTurma_PadraoDiretoriaECoordenacao(string perfil, bool esperado)
    {
        Assert.Equal(esperado, PermissoesPerfil.PodeGerarMapaTurma(new FakeCurrentUserService(null, perfil).TemPermissao));
    }

    [Fact]
    public void PermissaoNova_ChegaAoCoordenadorJaConfigurado_SemMexerNosOutros()
    {
        Assert.Equal(new[] { Permissoes.NotasMapaTurma }, Permissoes.NovasDesdeVersao(1, Perfis.Coordenador));
        Assert.Empty(Permissoes.NovasDesdeVersao(1, Perfis.Secretaria));
        Assert.Empty(Permissoes.NovasDesdeVersao(Permissoes.VersaoAtual, Perfis.Coordenador));
    }

    private static DisciplinaListItemDto Disciplina(string nome, string codigo) =>
        new(Guid.NewGuid(), nome, codigo, new[] { new DisciplinaSerieDto(SerieId, "6º Ano") }, 80, true);

    private static PeriodoListItemDto Periodo(int trimestre, DateTime inicio) =>
        new(Guid.NewGuid(), Ano, trimestre, $"{trimestre}º Trimestre", inicio, inicio.AddMonths(3), IsAberto: true, AbertoManualmente: false);

    private static AlunoListItemDto Aluno(string nome, bool ativo) =>
        new(Guid.NewGuid(), $"M-{nome}", null, "00000000000", nome, new DateTime(2014, 1, 1), Ano, SerieId, "6º Ano", Turma.Id, Turma.Nome, ativo);

    private static NotaListItemDto Nota(AlunoListItemDto aluno, DisciplinaListItemDto disciplina, PeriodoListItemDto periodo,
        decimal? av1, decimal? av2, decimal? av3, decimal? rec, decimal final) =>
        new(Guid.NewGuid(), aluno.Id, aluno.NomeCompleto, disciplina.Id, disciplina.Nome, Guid.NewGuid(), "Prof",
            periodo.Id, periodo.Descricao, Ano, periodo.Trimestre, av1, av2, av3, rec, (av1 ?? 0) + (av2 ?? 0) + (av3 ?? 0), final, true);
}

public sealed class MapaTurmaArquivosTests
{
    // Turma grande (12 disciplinas, 40 alunos): o PDF precisa dividir as disciplinas em blocos sem estourar o layout.
    [Fact]
    public void TurmaGrande_GeraExcelEPdfEmBlocos()
    {
        var disciplinas = Enumerable.Range(1, 12)
            .Select(i => new SistemaEscolar.Application.Notas.MapaDisciplinaDto(Guid.NewGuid(), $"Disciplina com nome longo {i:00}", $"D{i:00}"))
            .ToList();
        SistemaEscolar.Application.Notas.MapaCelulaDto Celula(int seed) => new(
            new[]
            {
                new SistemaEscolar.Application.Notas.MapaNotaDto(seed % 10, SistemaEscolar.Application.Notas.MapaNotaEstado.Lancada),
                new SistemaEscolar.Application.Notas.MapaNotaDto(null, SistemaEscolar.Application.Notas.MapaNotaEstado.Pendente),
                new SistemaEscolar.Application.Notas.MapaNotaDto(2.5m, SistemaEscolar.Application.Notas.MapaNotaEstado.Incompleta),
            },
            seed % 10);
        var alunos = Enumerable.Range(1, 40)
            .Select(i => new SistemaEscolar.Application.Notas.MapaAlunoDto(i, $"M{i}", $"Aluno com nome bem comprido número {i}", i == 40,
                disciplinas.Select((_, d) => Celula(i + d)).ToList(), new[] { "D01", "D02" }, 12, 12,
                "Abaixo da média em 2: D01, D02; 12 notas não lançadas; 12 notas incompletas"))
            .ToList();
        var resumo = disciplinas.Select(_ => new SistemaEscolar.Application.Notas.MapaResumoDisciplinaDto(
            new decimal?[] { 6.5m, null, null }, 6.5m, new[] { 3, 0, 0 }, 3, new[] { 0, 40, 0 })).ToList();
        var mapa = new SistemaEscolar.Application.Notas.MapaTurmaDto("9º ANO B", "9º Ano", "Matutino", 2026,
            new[] { "1º Trimestre", "2º Trimestre", "3º Trimestre" }, disciplinas, alunos, resumo, new DateTime(2026, 10, 29, 10, 0, 0));

        var excel = SistemaEscolar.Web.Pages.Notas.MapaTurmaArquivos.GerarExcel(mapa);
        var pdf = SistemaEscolar.Web.Pages.Notas.MapaTurmaArquivos.GerarPdf(mapa);

        Assert.NotEmpty(excel);
        using var documento = UglyToad.PdfPig.PdfDocument.Open(pdf);
        var texto = string.Join("\n", documento.GetPages().Select(p => p.Text));
        Assert.Contains("parte 2 de 2", texto);
        Assert.Contains("Disciplina com nome longo 12", texto);
    }
}
