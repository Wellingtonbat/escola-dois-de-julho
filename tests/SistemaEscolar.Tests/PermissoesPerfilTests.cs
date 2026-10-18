using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Tests.Support;
using Xunit;

namespace SistemaEscolar.Tests;

// As permissões padrão de cada perfil (gravadas na primeira inicialização) precisam reproduzir as regras que
// valiam antes da tela "Perfis e Permissões": ninguém pode ganhar nem perder acesso no dia da atualização.
public sealed class PermissoesPerfilTests
{
    private static Func<string, bool> Perfil(params string[] roles) =>
        new FakeCurrentUserService(null, roles).TemPermissao;

    [Fact]
    public void ViceDiretor_TemAsMesmasPermissoesDoDiretor()
    {
        Assert.Equal(Permissoes.PadraoDoPerfil(Perfis.Diretor), Permissoes.PadraoDoPerfil(Perfis.ViceDiretor));
        Assert.Equal(Permissoes.Todas.Count, Permissoes.PadraoDoPerfil(Perfis.Diretor).Count);
    }

    [Theory]
    [InlineData(Perfis.Coordenador)]
    [InlineData(Perfis.CoordenadorLegado)]
    [InlineData(Perfis.Secretaria)]
    public void CoordenadorESecretaria_ConsultamNotasMasNaoAlteram(string perfil)
    {
        var temPermissao = Perfil(perfil);

        Assert.True(PermissoesPerfil.PodeAcessarNotas(temPermissao));
        Assert.False(PermissoesPerfil.PodeAlterarNotas(temPermissao));
        Assert.False(PermissoesPerfil.TemVisaoRestrita(temPermissao));
        Assert.True(PermissoesPerfil.PodeBaixarBoletimPdf(temPermissao));
        Assert.True(PermissoesPerfil.PodeGerenciarAlunos(temPermissao));
        Assert.True(PermissoesPerfil.PodeGerenciarPeriodos(temPermissao));
        Assert.True(PermissoesPerfil.PodeGerenciarUsuarios(temPermissao));
        Assert.True(PermissoesPerfil.PodeAcessarAtas(temPermissao));
        Assert.False(PermissoesPerfil.PodeEditarAtaFinalizada(temPermissao));
        Assert.False(PermissoesPerfil.PodeLancarAvaliacaoFinal(temPermissao));
        Assert.False(PermissoesPerfil.PodeVerAuditoria(temPermissao));
        Assert.False(PermissoesPerfil.PodeGerenciarPerfis(temPermissao));
    }

    [Fact]
    public void Professor_AlteraNotasMasSoNoProprioEscopo()
    {
        var professor = Perfil(Perfis.Professor);

        Assert.True(PermissoesPerfil.PodeVerPainel(professor));
        Assert.True(PermissoesPerfil.PodeAlterarNotas(professor));
        Assert.True(PermissoesPerfil.TemVisaoRestrita(professor));
        Assert.True(PermissoesPerfil.AlteracaoDeNotasRestritaAoEscopo(professor));
        Assert.False(PermissoesPerfil.PodeLancarComPeriodoFechado(professor));
        Assert.False(PermissoesPerfil.PodeBaixarBoletimPdf(professor));
        Assert.False(PermissoesPerfil.PodeGerenciarAlunos(professor));
        Assert.False(PermissoesPerfil.PodeGerenciarUsuarios(professor));
    }

    [Fact]
    public void ProfessorQueEViceDiretor_EnxergaTudoENaoFicaRestritoAoEscopo()
    {
        var professorVice = Perfil(Perfis.Professor, Perfis.ViceDiretor);

        Assert.False(PermissoesPerfil.TemVisaoRestrita(professorVice));
        Assert.False(PermissoesPerfil.AlteracaoDeNotasRestritaAoEscopo(professorVice));
        Assert.True(PermissoesPerfil.PodeAlterarNotas(professorVice));
        Assert.True(PermissoesPerfil.PodeLancarComPeriodoFechado(professorVice));
        Assert.True(PermissoesPerfil.PodeAbrirFecharPeriodo(professorVice));
    }

    [Fact]
    public void ProfessorQueECoordenador_EnxergaComoGestaoMasSoAlteraNotasNoProprioEscopo()
    {
        var professorCoordenador = Perfil(Perfis.Professor, Perfis.Coordenador);

        Assert.False(PermissoesPerfil.TemVisaoRestrita(professorCoordenador));
        Assert.True(PermissoesPerfil.PodeAlterarNotas(professorCoordenador));
        Assert.True(PermissoesPerfil.AlteracaoDeNotasRestritaAoEscopo(professorCoordenador));
    }

    [Theory]
    [InlineData(Perfis.Diretor, true)]
    [InlineData(Perfis.ViceDiretor, true)]
    [InlineData(Perfis.Coordenador, true)]
    [InlineData(Perfis.Secretaria, false)]
    [InlineData(Perfis.Professor, false)]
    public void AbrirEFecharPeriodo_PadraoDiretoriaECoordenacao(string perfil, bool esperado)
    {
        Assert.Equal(esperado, PermissoesPerfil.PodeAbrirFecharPeriodo(Perfil(perfil)));
    }

    [Theory]
    [InlineData(Perfis.Coordenador)]
    [InlineData(Perfis.Secretaria)]
    [InlineData(Perfis.Professor)]
    public void ContasDaDiretoria_NaoSaoCobertasPorPerfisMenores(string perfilDoUsuario)
    {
        var temPermissao = Perfil(perfilDoUsuario);

        Assert.False(PermissoesPerfil.CobrePermissoes(temPermissao, Permissoes.PadraoDoPerfil(Perfis.Diretor)));
        Assert.False(PermissoesPerfil.CobrePermissoes(temPermissao, Permissoes.PadraoDoPerfil(Perfis.ViceDiretor)));
        Assert.False(PermissoesPerfil.PodeVerAuditoria(temPermissao));
    }

    [Fact]
    public void CobrePermissoes_SoQuemTemTodasAsPermissoesDoOutroPerfil()
    {
        Assert.True(PermissoesPerfil.CobrePermissoes(Perfil(Perfis.ViceDiretor), Permissoes.PadraoDoPerfil(Perfis.Diretor)));
        Assert.True(PermissoesPerfil.CobrePermissoes(Perfil(Perfis.Coordenador), Permissoes.PadraoDoPerfil(Perfis.Secretaria)));
        // A Secretaria não tem "abrir e fechar períodos": não pode administrar contas de Coordenador.
        Assert.False(PermissoesPerfil.CobrePermissoes(Perfil(Perfis.Secretaria), Permissoes.PadraoDoPerfil(Perfis.Coordenador)));
        Assert.False(PermissoesPerfil.CobrePermissoes(Perfil(Perfis.Professor), Permissoes.PadraoDoPerfil(Perfis.Secretaria)));
    }

    [Fact]
    public void Descrever_UsaNomesAmigaveis()
    {
        Assert.Equal("Vice-Diretor", Perfis.Descrever(Perfis.ViceDiretor));
        Assert.Equal("Secretária", Perfis.Descrever(Perfis.Secretaria));
        Assert.Equal("Diretor", Perfis.Descrever(Perfis.Diretor));
        Assert.Equal("Abrir e fechar períodos", Permissoes.Descrever(Permissoes.PeriodosAbrirFechar));
    }

    [Fact]
    public void Catalogo_NaoTemCodigosRepetidos()
    {
        Assert.Equal(Permissoes.Todas.Count, Permissoes.Todas.Select(x => x.Codigo).Distinct().Count());
    }

    [Fact]
    public void PerfilSemPermissoes_NaoEnxergaNada()
    {
        var semPermissao = Perfil("Perfil Novo");

        Assert.True(PermissoesPerfil.TemVisaoRestrita(semPermissao));
        Assert.False(PermissoesPerfil.PodeVerPainel(semPermissao));
        Assert.False(PermissoesPerfil.PodeAcessarNotas(semPermissao));
    }

    [Theory]
    [InlineData(Permissoes.NotasVer)]
    [InlineData(Permissoes.NotasLancar)]
    [InlineData(Permissoes.AvaliacaoFinalLancar)]
    public void QuemLancaNotasOuAvaliacaoFinal_TambemAbreAsTelasDeNotas(string permissao)
    {
        Func<string, bool> temPermissao = p => p == permissao;

        Assert.True(PermissoesPerfil.PodeAcessarNotas(temPermissao));
        Assert.False(PermissoesPerfil.PodeAcessarNotas(p => p == Permissoes.BoletinsPdf));
    }

    [Theory]
    [InlineData(Perfis.Diretor)]
    [InlineData(Perfis.ViceDiretor)]
    [InlineData(Perfis.Coordenador)]
    [InlineData(Perfis.Secretaria)]
    [InlineData(Perfis.Professor)]
    public void ExclusaoDePeriodo_DesativadaParaTodosOsPerfis(string perfil)
    {
        Assert.False(PermissoesPerfil.PodeExcluirPeriodo(Perfil(perfil)));
    }
}
