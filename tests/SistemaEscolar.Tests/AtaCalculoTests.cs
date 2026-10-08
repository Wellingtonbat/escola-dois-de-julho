using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Atas;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Tests.Support;
using Xunit;

namespace SistemaEscolar.Tests;

public sealed class AtaCalculoTests
{
    private static readonly AtaDisciplinaColunaDto Matematica = new(Guid.NewGuid(), "Matemática", "Mat.");
    private static readonly AtaDisciplinaColunaDto Historia = new(Guid.NewGuid(), "História", "Hist.");
    private static readonly AtaDisciplinaColunaDto EstudosLiterarios = new(Guid.NewGuid(), "Estudos Literários", "Est. Lit.");
    private static readonly AtaDisciplinaColunaDto[] Colunas = { EstudosLiterarios, Historia, Matematica };

    // Disciplinas com lançamento na turma (Estudos Literários não é ofertada: ninguém tem nota).
    private static readonly IReadOnlySet<Guid> ComLancamento = new HashSet<Guid> { Matematica.DisciplinaId, Historia.DisciplinaId };

    private static readonly Guid AlunoId = Guid.NewGuid();

    [Fact]
    public void TodasAprovadas_Aprovado_ComDisciplinaNaoOfertadaEmBranco()
    {
        var calculo = AtaCalculo.Calcular(Colunas, new[] { Resultado(Matematica, 7m, "Aprovado"), Resultado(Historia, 5m, "Aprovado") }, ComLancamento);

        Assert.Equal(AtaConstantes.RFAprovado, calculo.RF);
        Assert.Null(calculo.Pendencia);
        Assert.Equal(2, calculo.Disciplinas.Count);
        Assert.DoesNotContain(calculo.Disciplinas, d => d.DisciplinaId == EstudosLiterarios.DisciplinaId);
    }

    [Fact]
    public void AbaixoDaMediaSemAvaliacaoFinal_Pendente()
    {
        var calculo = AtaCalculo.Calcular(Colunas, new[] { Resultado(Matematica, 3.5m, "Reprovado"), Resultado(Historia, 6m, "Aprovado") }, ComLancamento);

        Assert.Equal(AtaConstantes.RFPendente, calculo.RF);
        Assert.Contains("Avaliação Final", calculo.Pendencia);
        Assert.Contains("Matemática", calculo.Pendencia);
    }

    [Fact]
    public void AvaliacaoFinalAtingiuAMedia_Aprovado()
    {
        // Resultados já aplica "vale a maior": média 3,5 com Avaliação Final 6,0 chega como Aprovado / 6,0.
        var calculo = AtaCalculo.Calcular(Colunas, new[] { Resultado(Matematica, 6m, "Aprovado", avaliacaoFinal: 6m), Resultado(Historia, 6m, "Aprovado") }, ComLancamento);

        Assert.Equal(AtaConstantes.RFAprovado, calculo.RF);
        Assert.Equal(6m, calculo.Disciplinas.Single(d => d.DisciplinaId == Matematica.DisciplinaId).ResultadoFinalAno);
    }

    [Fact]
    public void AbaixoDaMediaMesmoComAvaliacaoFinal_Conservado()
    {
        var calculo = AtaCalculo.Calcular(Colunas, new[] { Resultado(Matematica, 4m, "Reprovado", avaliacaoFinal: 4m), Resultado(Historia, 6m, "Aprovado") }, ComLancamento);

        Assert.Equal(AtaConstantes.RFConservado, calculo.RF);
        Assert.Null(calculo.Pendencia);
    }

    [Fact]
    public void TrimestresIncompletos_Pendente()
    {
        var calculo = AtaCalculo.Calcular(Colunas, new[] { Resultado(Matematica, 7m, "Pendente"), Resultado(Historia, 6m, "Aprovado") }, ComLancamento);

        Assert.Equal(AtaConstantes.RFPendente, calculo.RF);
        Assert.Contains("Notas incompletas: Matemática", calculo.Pendencia);
    }

    [Fact]
    public void SemNotaEmDisciplinaQueATurmaTem_Pendente()
    {
        var calculo = AtaCalculo.Calcular(Colunas, new[] { Resultado(Historia, 6m, "Aprovado") }, ComLancamento);

        Assert.Equal(AtaConstantes.RFPendente, calculo.RF);
        Assert.Contains("Matemática", calculo.Pendencia);
    }

    [Fact]
    public void ConservadoMasComOutraDisciplinaPendente_FicaPendente()
    {
        var calculo = AtaCalculo.Calcular(Colunas, new[] { Resultado(Matematica, 4m, "Reprovado", avaliacaoFinal: 4m), Resultado(Historia, 3m, "Reprovado") }, ComLancamento);

        Assert.Equal(AtaConstantes.RFPendente, calculo.RF);
    }

    private static readonly SerieListItemDto Sexto = new(Guid.NewGuid(), "6º Ano", 6, true);
    private static readonly SerieListItemDto Setimo = new(Guid.NewGuid(), "7º Ano", 7, true);
    private static readonly SerieListItemDto Nono = new(Guid.NewGuid(), "9º Ano", 9, true);
    private static readonly SerieListItemDto[] Series = { Nono, Setimo, Sexto };

    [Theory]
    [InlineData(AtaConstantes.RFAprovado, "7º Ano")]
    [InlineData(AtaConstantes.RFConservado, "6º Ano")]
    [InlineData(AtaConstantes.RFTransferido, "6º Ano")]
    [InlineData(AtaConstantes.RFDeixouDeFrequentar, "6º Ano")]
    [InlineData(AtaConstantes.RFPendente, null)]
    public void AptoACursar_PeloResultado(string rf, string? esperado)
    {
        Assert.Equal(esperado, AtaCalculo.AptoACursar(rf, Sexto.Id, Series));
    }

    [Fact]
    public void AptoACursar_UltimaSerieAprovada_FicaEmBranco()
    {
        Assert.Null(AtaCalculo.AptoACursar(AtaConstantes.RFAprovado, Nono.Id, Series));
    }

    [Theory]
    [InlineData(Perfis.Diretor, true)]
    [InlineData(Perfis.ViceDiretor, true)]
    [InlineData(Perfis.Professor, false)]
    [InlineData(Perfis.Coordenador, false)]
    [InlineData(Perfis.Secretaria, false)]
    public void AvaliacaoFinal_SoADiretoriaLanca(string perfil, bool podeLancar)
    {
        Assert.Equal(podeLancar, PermissoesPerfil.PodeLancarAvaliacaoFinal(new FakeCurrentUserService(null, perfil).IsInRole));
    }

    private static ResultadoAcademicoDto Resultado(AtaDisciplinaColunaDto disciplina, decimal resultadoFinal, string situacao, decimal? avaliacaoFinal = null) =>
        new(AlunoId, "Aluno", disciplina.DisciplinaId, disciplina.Nome, "6º Ano A", "6º Ano", "Professor", 2026,
            avaliacaoFinal.HasValue ? 3.5m : resultadoFinal, avaliacaoFinal, situacao != "Pendente", resultadoFinal, situacao, string.Empty);
}
