using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Assistente;
using SistemaEscolar.Tests.Support;
using Xunit;

namespace SistemaEscolar.Tests;

public sealed class AssistenteTests
{
    private static AssistenteService Elson(params string[] perfis) => new(new FakeCurrentUserService(null, perfis), "Elson");

    // Perguntas escritas do jeito que as pessoas costumam digitar (sem acento, com sinônimos, frases soltas).
    [Theory]
    [InlineData("como fecho o trimestre?", "periodo-fechar")]
    [InlineData("quero trancar as notas do 2 trimestre", "periodo-fechar")]
    [InlineData("encerrar periodo", "periodo-fechar")]
    [InlineData("como libero o lançamento depois do prazo", "periodo-abrir")]
    [InlineData("reabrir o trimestre", "periodo-abrir")]
    [InlineData("como cadastrar aluno novo", "aluno-cadastrar")]
    [InlineData("matricular um aluno", "aluno-cadastrar")]
    [InlineData("importar planilha de alunos", "aluno-importar")]
    [InlineData("o aluno foi transferido, o que faço?", "aluno-saiu")]
    [InlineData("qual a nota da Ana no primeiro trimestre de matematica", "ver-nota-aluno")]
    [InlineData("quanto o joão tirou em português", "ver-nota-aluno")]
    [InlineData("lançamento em massa", "nota-lancar-massa")]
    [InlineData("como lanço as notas da turma toda", "nota-lancar-massa")]
    [InlineData("nao consigo lancar nota", "nao-consigo-lancar")]
    [InlineData("como é calculada a média", "nota-calculo")]
    [InlineData("recuperação paralela", "rec-paralela")]
    [InlineData("como imprimo os boletins da turma", "boletins-turma")]
    [InlineData("gerar mapa da turma em excel", "mapa-turma")]
    [InlineData("o que significa o traço no mapa", "mapa-ler")]
    [InlineData("como lanço a recuperação final", "avaliacao-final")]
    [InlineData("prova final", "avaliacao-final")]
    [InlineData("criar ata", "ata-criar")]
    [InlineData("a ata não atualizou as notas", "ata-atualizar")]
    [InlineData("como edito uma ata finalizada", "ata-reabrir")]
    [InlineData("esqueci minha senha", "esqueci-senha")]
    [InlineData("resetar senha do professor", "professor-senha")]
    [InlineData("criar login para a secretaria", "usuario-criar")]
    [InlineData("professora virou coordenadora", "professor-vira-coordenador")]
    [InlineData("tirar permissão do coordenador", "perfil-editar")]
    [InlineData("quem alterou a nota do aluno", "auditoria")]
    [InlineData("vincular professor a turma", "professor-vinculo")]
    [InlineData("cadastrar disciplina", "disciplina-cadastrar")]
    [InlineData("baixar o manual", "manual")]
    [InlineData("quais professores ainda nao lançaram nota", "pendencias-trimestre")]
    [InlineData("o que é aprovação geral no painel", "dashboard-cartoes")]
    public void Pergunta_EncontraOTopicoCerto(string pergunta, string esperado)
    {
        var resposta = Elson(Perfis.Diretor).Responder(pergunta, null, "Diretor");

        Assert.True(resposta.Encontrou, $"Não encontrou nada para \"{pergunta}\"");
        Assert.Equal(esperado, resposta.TopicoId);
    }

    [Fact]
    public void SemPermissao_AvisaEEscondeOAtalho()
    {
        var resposta = Elson(Perfis.Coordenador).Responder("como lanço a avaliação final", null, "Coordenador");

        Assert.Equal("avaliacao-final", resposta.TopicoId);
        Assert.Contains("Seu perfil (Coordenador) não tem a permissão \"Lançar a Avaliação Final\"", resposta.Aviso);
        Assert.Null(resposta.Link);
        Assert.NotEmpty(resposta.Passos);
    }

    [Fact]
    public void ComPermissao_SemAviso_ComAtalho()
    {
        var resposta = Elson(Perfis.Coordenador).Responder("como fecho o trimestre", null, "Coordenador");

        Assert.Null(resposta.Aviso);
        Assert.Equal("/Periodos", resposta.Link?.Pagina);
        Assert.Equal(9, resposta.SecaoManual);
    }

    [Theory]
    [InlineData("qual a previsão do tempo amanhã em salvador")]
    [InlineData("me conta uma piada")]
    [InlineData("quem descobriu o brasil")]
    [InlineData("qual o horário do recreio")]
    public void PerguntaSemRelacao_NaoInventaResposta(string pergunta)
    {
        var resposta = Elson(Perfis.Professor).Responder(pergunta, null, "Professor");

        Assert.False(resposta.Encontrou);
        Assert.Contains("Não encontrei", resposta.Texto);
        Assert.NotEmpty(resposta.Sugestoes);
    }

    [Fact]
    public void CumprimentoEAgradecimento_TemRespostaPropria()
    {
        Assert.Contains("Por nada", Elson(Perfis.Professor).Responder("obrigado!", null, "Professor").Texto);
        Assert.Contains("Me diga o que você quer fazer", Elson(Perfis.Professor).Responder("oi elson", null, "Professor").Texto);
    }

    [Fact]
    public void BoasVindas_ComONomeDoAssistente()
    {
        var boasVindas = Elson(Perfis.Professor).BoasVindas("Juliana", "Professor");

        Assert.Equal("Olá, Juliana! Me chamo Elson, sou seu assistente.", boasVindas.Saudacao);
        Assert.InRange(boasVindas.Frequentes.Count, 1, 6);
    }

    [Fact]
    public void ClicarNumaSugestao_RespondeAqueleTopico()
    {
        var resposta = Elson(Perfis.Diretor).Responder(null, "mapa-turma", "Diretor");

        Assert.Equal("mapa-turma", resposta.TopicoId);
    }

    [Fact]
    public void BaseDeConhecimento_ConsistenteComOSistema()
    {
        var topicos = AssistenteBaseConhecimento.Topicos;
        Assert.True(topicos.Count >= 40);
        Assert.Equal(topicos.Count, topicos.Select(t => t.Id).Distinct().Count());

        // Toda permissão citada existe no catálogo.
        var desconhecidas = topicos.SelectMany(t => t.Permissoes ?? Array.Empty<string>()).Where(p => !Permissoes.Existe(p)).ToList();
        Assert.Empty(desconhecidas);

        // Todo atalho aponta para uma página que existe.
        var raizPages = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/SistemaEscolar.Web/Pages"));
        foreach (var link in topicos.Select(t => t.Link).Where(l => l is not null))
        {
            var caminho = link!.Pagina.Split('?')[0].Trim('/');
            var arquivo = Path.Combine(raizPages, caminho.Length == 0 ? "Index.cshtml" : caminho.Replace('/', Path.DirectorySeparatorChar) + (Directory.Exists(Path.Combine(raizPages, caminho)) ? "/Index.cshtml" : ".cshtml"));
            Assert.True(File.Exists(arquivo), $"Página do atalho não existe: {link.Pagina}");
        }
    }
}
