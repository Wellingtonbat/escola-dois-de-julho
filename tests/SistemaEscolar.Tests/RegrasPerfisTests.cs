using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Seguranca;
using SistemaEscolar.Tests.Support;
using Xunit;

namespace SistemaEscolar.Tests;

public sealed class RegrasPerfisTests
{
    private static Func<string, bool> Perfil(params string[] roles) =>
        new FakeCurrentUserService(null, roles).TemPermissao;

    // Usuário que gerencia perfis, mas não tem tudo (ex.: Coordenador a quem se deu "Gerenciar perfis").
    private static Func<string, bool> CoordenadorQueGerenciaPerfis =>
        p => p == Permissoes.PerfisGerenciar || Permissoes.PadraoDoPerfil(Perfis.Coordenador).Contains(p);

    private static IReadOnlyList<string> Padrao(string perfil) => Permissoes.PadraoDoPerfil(perfil);

    [Fact]
    public void ViceDiretor_RetiraDoCoordenadorAbrirEFecharPeriodos()
    {
        var novas = Padrao(Perfis.Coordenador).Where(x => x != Permissoes.PeriodosAbrirFechar).ToList();

        var erro = RegrasPerfis.ValidarAlteracao(Perfil(Perfis.ViceDiretor), Perfis.Coordenador, Padrao(Perfis.Coordenador), novas, usuarioPertenceAoPerfil: false);

        Assert.Null(erro);
    }

    [Fact]
    public void PerfilDiretor_NaoPodeSerAlterado()
    {
        var erro = RegrasPerfis.ValidarAlteracao(Perfil(Perfis.Diretor), Perfis.Diretor, Padrao(Perfis.Diretor), Array.Empty<string>(), usuarioPertenceAoPerfil: true);

        Assert.NotNull(erro);
        Assert.False(RegrasPerfis.PodeEditar(Perfil(Perfis.Diretor), Perfis.Diretor, Padrao(Perfis.Diretor)));
    }

    [Fact]
    public void NaoConcedePermissaoQueNaoTem()
    {
        var novas = Padrao(Perfis.Secretaria).Append(Permissoes.AuditoriaVer).ToList();

        var erro = RegrasPerfis.ValidarAlteracao(CoordenadorQueGerenciaPerfis, Perfis.Secretaria, Padrao(Perfis.Secretaria), novas, usuarioPertenceAoPerfil: false);

        Assert.NotNull(erro);
        Assert.Contains("Consultar a auditoria", erro);
        Assert.NotNull(RegrasPerfis.ValidarCriacao(CoordenadorQueGerenciaPerfis, new[] { Permissoes.AuditoriaVer }));
    }

    [Fact]
    public void NaoAlteraPerfilQueTemMaisPermissoesQueAsSuas()
    {
        // Retirar algo do Vice-Diretor seria "rebaixar" quem tem mais acesso.
        var novas = Padrao(Perfis.ViceDiretor).Where(x => x != Permissoes.AuditoriaVer).ToList();

        var erro = RegrasPerfis.ValidarAlteracao(CoordenadorQueGerenciaPerfis, Perfis.ViceDiretor, Padrao(Perfis.ViceDiretor), novas, usuarioPertenceAoPerfil: false);

        Assert.NotNull(erro);
        Assert.False(RegrasPerfis.PodeEditar(CoordenadorQueGerenciaPerfis, Perfis.ViceDiretor, Padrao(Perfis.ViceDiretor)));
        Assert.True(RegrasPerfis.PodeEditar(CoordenadorQueGerenciaPerfis, Perfis.Secretaria, Padrao(Perfis.Secretaria)));
    }

    [Fact]
    public void NaoRetiraGerenciarPerfisDoProprioPerfil()
    {
        var novas = Padrao(Perfis.ViceDiretor).Where(x => x != Permissoes.PerfisGerenciar).ToList();

        Assert.NotNull(RegrasPerfis.ValidarAlteracao(Perfil(Perfis.ViceDiretor), Perfis.ViceDiretor, Padrao(Perfis.ViceDiretor), novas, usuarioPertenceAoPerfil: true));
        Assert.Null(RegrasPerfis.ValidarAlteracao(Perfil(Perfis.Diretor), Perfis.ViceDiretor, Padrao(Perfis.ViceDiretor), novas, usuarioPertenceAoPerfil: false));
    }

    [Fact]
    public void SemPermissaoDeGerenciarPerfis_NaoFazNada()
    {
        var coordenador = Perfil(Perfis.Coordenador);

        Assert.NotNull(RegrasPerfis.ValidarCriacao(coordenador, Array.Empty<string>()));
        Assert.NotNull(RegrasPerfis.ValidarAlteracao(coordenador, Perfis.Secretaria, Padrao(Perfis.Secretaria), Padrao(Perfis.Secretaria), false));
        Assert.NotNull(RegrasPerfis.ValidarExclusao(coordenador, "Auxiliar", Array.Empty<string>(), 0));
    }

    [Fact]
    public void Exclusao_SoDePerfilCriadoESemUsuarios()
    {
        var vice = Perfil(Perfis.ViceDiretor);

        Assert.Null(RegrasPerfis.ValidarExclusao(vice, "Auxiliar de Secretaria", new[] { Permissoes.NotasVer }, 0));
        Assert.NotNull(RegrasPerfis.ValidarExclusao(vice, "Auxiliar de Secretaria", new[] { Permissoes.NotasVer }, 2));
        Assert.NotNull(RegrasPerfis.ValidarExclusao(vice, Perfis.Secretaria, Padrao(Perfis.Secretaria), 0));
        Assert.False(RegrasPerfis.PodeExcluir(vice, Perfis.Professor, Padrao(Perfis.Professor), 0));
    }

    [Fact]
    public void Criacao_ComPermissaoDesconhecida_ERecusada()
    {
        Assert.NotNull(RegrasPerfis.ValidarCriacao(Perfil(Perfis.Diretor), new[] { "Sistema.Tudo" }));
    }

    [Theory]
    [InlineData("", "Informe o nome do perfil.")]
    [InlineData("   ", "Informe o nome do perfil.")]
    [InlineData("secretaria", "Já existe um perfil com este nome.")]
    [InlineData("Secretária", "Já existe um perfil com este nome.")]
    [InlineData("vice-diretor", "Já existe um perfil com este nome.")]
    [InlineData("Auxiliar de Secretaria", null)]
    public void ValidarNome(string nome, string? esperado)
    {
        var existentes = new[] { (Guid.NewGuid(), Perfis.Secretaria), (Guid.NewGuid(), Perfis.ViceDiretor) };

        Assert.Equal(esperado, RegrasPerfis.ValidarNome(nome, null, existentes));
    }

    [Fact]
    public void ValidarNome_ProprioPerfilNaoContaComoRepetido()
    {
        var id = Guid.NewGuid();

        Assert.Null(RegrasPerfis.ValidarNome("Auxiliar", id, new[] { (id, "Auxiliar") }));
        Assert.NotNull(RegrasPerfis.ValidarNome(new string('x', RegrasPerfis.TamanhoMaximoNome + 1), null, Array.Empty<(Guid, string)>()));
    }
}
