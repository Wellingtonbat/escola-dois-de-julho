using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Tests.Support;
using Xunit;

namespace SistemaEscolar.Tests;

public sealed class PermissoesPerfilTests
{
    private static Func<string, bool> Perfil(params string[] roles) =>
        new FakeCurrentUserService(null, roles).IsInRole;

    [Fact]
    public void ViceDiretor_TemAsMesmasPermissoesDoDiretor()
    {
        var diretor = Perfil(Perfis.Diretor);
        var vice = Perfil(Perfis.ViceDiretor);

        Assert.Equal(PermissoesPerfil.EhDiretoria(diretor), PermissoesPerfil.EhDiretoria(vice));
        Assert.Equal(PermissoesPerfil.EhGestao(diretor), PermissoesPerfil.EhGestao(vice));
        Assert.Equal(PermissoesPerfil.PodeAlterarNotas(diretor), PermissoesPerfil.PodeAlterarNotas(vice));
        Assert.Equal(PermissoesPerfil.PodeAbrirFecharPeriodo(diretor), PermissoesPerfil.PodeAbrirFecharPeriodo(vice));
        Assert.Equal(PermissoesPerfil.PodeExcluirPeriodo(diretor), PermissoesPerfil.PodeExcluirPeriodo(vice));
        Assert.Equal(PermissoesPerfil.PodeVerAuditoria(diretor), PermissoesPerfil.PodeVerAuditoria(vice));
        Assert.True(PermissoesPerfil.PodeGerenciarContaComPerfil(vice, Perfis.Diretor));
    }

    [Theory]
    [InlineData(Perfis.Coordenador)]
    [InlineData(Perfis.CoordenadorLegado)]
    [InlineData(Perfis.Secretaria)]
    public void CoordenadorESecretaria_ConsultamNotasMasNaoAlteram(string perfil)
    {
        var isInRole = Perfil(perfil);

        Assert.True(PermissoesPerfil.PodeAcessarNotas(isInRole));
        Assert.False(PermissoesPerfil.PodeAlterarNotas(isInRole));
    }

    [Fact]
    public void Professor_AlteraNotasMasSoNoProprioEscopo()
    {
        var professor = Perfil(Perfis.Professor);

        Assert.True(PermissoesPerfil.PodeAlterarNotas(professor));
        Assert.True(PermissoesPerfil.EhApenasProfessor(professor));
        Assert.True(PermissoesPerfil.AlteracaoDeNotasRestritaAoEscopo(professor));
        Assert.False(PermissoesPerfil.EhGestao(professor));
    }

    [Fact]
    public void ProfessorQueEViceDiretor_EnxergaTudoENaoFicaRestritoAoEscopo()
    {
        var professorVice = Perfil(Perfis.Professor, Perfis.ViceDiretor);

        Assert.True(PermissoesPerfil.EhDiretoria(professorVice));
        Assert.True(PermissoesPerfil.EhGestao(professorVice));
        Assert.False(PermissoesPerfil.EhApenasProfessor(professorVice));
        Assert.False(PermissoesPerfil.AlteracaoDeNotasRestritaAoEscopo(professorVice));
        Assert.True(PermissoesPerfil.PodeAlterarNotas(professorVice));
        Assert.True(PermissoesPerfil.PodeAbrirFecharPeriodo(professorVice));
    }

    [Fact]
    public void ProfessorQueECoordenador_EnxergaComoGestaoMasSoAlteraNotasNoProprioEscopo()
    {
        var professorCoordenador = Perfil(Perfis.Professor, Perfis.Coordenador);

        Assert.True(PermissoesPerfil.EhGestao(professorCoordenador));
        Assert.False(PermissoesPerfil.EhApenasProfessor(professorCoordenador));
        Assert.True(PermissoesPerfil.PodeAlterarNotas(professorCoordenador));
        Assert.True(PermissoesPerfil.AlteracaoDeNotasRestritaAoEscopo(professorCoordenador));
    }

    [Theory]
    [InlineData(Perfis.Diretor, true)]
    [InlineData(Perfis.ViceDiretor, true)]
    [InlineData(Perfis.Coordenador, true)]
    [InlineData(Perfis.Secretaria, false)]
    [InlineData(Perfis.Professor, false)]
    public void AbrirEFecharPeriodo_SoDiretoriaECoordenacao(string perfil, bool esperado)
    {
        Assert.Equal(esperado, PermissoesPerfil.PodeAbrirFecharPeriodo(Perfil(perfil)));
    }

    [Theory]
    [InlineData(Perfis.Coordenador)]
    [InlineData(Perfis.Secretaria)]
    [InlineData(Perfis.Professor)]
    public void ContasDeDiretoria_SoSaoGerenciadasPelaDiretoria(string perfilDoUsuario)
    {
        var isInRole = Perfil(perfilDoUsuario);

        Assert.False(PermissoesPerfil.PodeGerenciarContaComPerfil(isInRole, Perfis.Diretor));
        Assert.False(PermissoesPerfil.PodeGerenciarContaComPerfil(isInRole, Perfis.ViceDiretor));
        Assert.False(PermissoesPerfil.PodeVerAuditoria(isInRole));
    }

    [Fact]
    public void ContasAdministrativasComuns_SaoGerenciadasPorQualquerPerfilDeGestao()
    {
        Assert.True(PermissoesPerfil.PodeGerenciarContaComPerfil(Perfil(Perfis.Secretaria), Perfis.Coordenador));
        Assert.True(PermissoesPerfil.PodeGerenciarContaComPerfil(Perfil(Perfis.Coordenador), Perfis.Secretaria));
        Assert.False(PermissoesPerfil.PodeGerenciarContaComPerfil(Perfil(Perfis.Professor), Perfis.Secretaria));
    }

    [Fact]
    public void Descrever_UsaNomesAmigaveis()
    {
        Assert.Equal("Vice-Diretor", Perfis.Descrever(Perfis.ViceDiretor));
        Assert.Equal("Secretária", Perfis.Descrever(Perfis.Secretaria));
        Assert.Equal("Diretor", Perfis.Descrever(Perfis.Diretor));
    }
}
