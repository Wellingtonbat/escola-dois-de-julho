using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SistemaEscolar.Application.Abstractions;

namespace SistemaEscolar.Application.Assistente;

// Assistente da tela (Elson): responde dúvidas de uso com base nos tópicos de base-conhecimento.json, tirados do
// manual. Não usa IA nem serviço externo e não consulta dados de alunos: só encontra o tópico mais parecido com a
// pergunta e avisa quando o perfil de quem pergunta não tem a permissão daquela ação.

public sealed record AssistenteLink(string Pagina, string Rotulo);

public sealed record AssistenteTopico(
    string Id,
    string Titulo,
    IReadOnlyList<string>? Perguntas,
    IReadOnlyList<string>? PalavrasChave,
    string Resposta,
    IReadOnlyList<string>? Passos,
    string? Observacao,
    IReadOnlyList<string>? Permissoes,
    AssistenteLink? Link,
    int? SecaoManual,
    bool Frequente);

public sealed record AssistenteSugestaoDto(string Id, string Titulo);

public sealed record AssistenteRespostaDto(
    bool Encontrou,
    string? TopicoId,
    string Texto,
    IReadOnlyList<string> Passos,
    string? Observacao,
    string? Aviso,
    AssistenteLink? Link,
    int? SecaoManual,
    IReadOnlyList<AssistenteSugestaoDto> Sugestoes);

public sealed record AssistenteBoasVindasDto(string Saudacao, string Perfil, IReadOnlyList<AssistenteSugestaoDto> Frequentes);

public static class AssistenteBaseConhecimento
{
    private static readonly Lazy<IReadOnlyList<AssistenteTopico>> Carregados = new(Carregar);

    public static IReadOnlyList<AssistenteTopico> Topicos => Carregados.Value;

    private static IReadOnlyList<AssistenteTopico> Carregar()
    {
        var assembly = typeof(AssistenteBaseConhecimento).Assembly;
        var recurso = assembly.GetManifestResourceNames().Single(n => n.EndsWith("base-conhecimento.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(recurso)!;
        return JsonSerializer.Deserialize<List<AssistenteTopico>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new List<AssistenteTopico>();
    }
}

// Busca por palavras, sem IA: normaliza (minúsculas, sem acento), troca sinônimos, reduz cada palavra ao radical
// ("fecho", "fechar", "fechamento" -> "fech") e pontua os tópicos pelas palavras em comum, dando mais peso ao título,
// às perguntas de exemplo e às palavras-chave, e menos às palavras muito comuns na base (IDF).
public static class AssistenteBusca
{
    private const double PontuacaoMinima = 2.5;

    private static readonly HashSet<string> PalavrasVazias = new(StringComparer.Ordinal)
    {
        "a", "o", "as", "os", "um", "uma", "uns", "umas", "de", "do", "da", "dos", "das", "em", "no", "na", "nos", "nas",
        "por", "para", "pra", "pro", "com", "sem", "e", "ou", "que", "se", "eu", "me", "meu", "minha", "meus", "minhas",
        "voce", "seu", "sua", "ele", "ela", "isso", "isto", "esse", "essa", "este", "esta", "aqui", "ai", "la", "ao", "aos",
        "como", "faco", "fazer", "faz", "posso", "pode", "consigo", "onde", "fica", "ficar", "quero", "queria", "preciso",
        "gostaria", "saber", "sistema", "tela", "favor", "ja", "mais", "muito", "ser", "esta", "estou", "tem", "ter", "tenho",
        "qual", "quais", "quando", "ate", "sobre", "num", "numa", "elson", "oi", "ola", "bom", "boa", "dia", "tarde", "noite",
    };

    // Sinônimos de uso comum na escola, aplicados antes do radical.
    private static readonly Dictionary<string, string> Sinonimos = new(StringComparer.Ordinal)
    {
        ["trancar"] = "fechar", ["travar"] = "fechar", ["encerrar"] = "fechar", ["bloquear"] = "fechar", ["encerro"] = "fechar",
        ["destrancar"] = "abrir", ["destravar"] = "abrir", ["desbloquear"] = "abrir", ["liberar"] = "abrir", ["libero"] = "abrir",
        ["incluir"] = "cadastrar", ["adicionar"] = "cadastrar", ["inserir"] = "cadastrar", ["registrar"] = "cadastrar",
        ["matricular"] = "cadastrar", ["criar"] = "cadastrar", ["crio"] = "cadastrar", ["cadastro"] = "cadastrar",
        ["digitar"] = "lancar", ["digito"] = "lancar", ["colocar"] = "lancar", ["coloco"] = "lancar", ["lanco"] = "lancar",
        ["materia"] = "disciplina", ["materias"] = "disciplina",
        ["resetar"] = "redefinir", ["reseto"] = "redefinir", ["recuperar"] = "redefinir", ["redefino"] = "redefinir",
        ["unidade"] = "trimestre", ["bimestre"] = "trimestre",
        ["prova"] = "avaliacao", ["exame"] = "avaliacao",
        ["imprimir"] = "baixar", ["imprimo"] = "baixar", ["baixo"] = "baixar", ["gerar"] = "baixar", ["gero"] = "baixar", ["emitir"] = "baixar",
        ["xlsx"] = "excel", ["planilha"] = "excel",
        ["logar"] = "entrar", ["login"] = "entrar", ["acessar"] = "entrar", ["entro"] = "entrar",
        ["coordenadora"] = "coordenador", ["secretario"] = "secretaria",
    };

    private static readonly string[] Sufixos =
    {
        "amentos", "imentos", "amento", "imento", "mente", "acoes", "icoes", "coes", "cao", "sao", "ados", "adas", "idos", "idas",
        "ando", "endo", "indo", "ado", "ada", "ido", "ida", "ar", "er", "ir", "as", "es", "is", "os", "a", "e", "o", "s",
    };

    public static IReadOnlyList<(AssistenteTopico Topico, double Pontos)> Buscar(string? pergunta, IReadOnlyList<AssistenteTopico> topicos)
    {
        var termos = Termos(pergunta).Distinct().ToList();
        if (termos.Count == 0)
        {
            return Array.Empty<(AssistenteTopico, double)>();
        }

        var indices = topicos.Select(Indexar).ToList();
        var total = topicos.Count;
        var idf = termos.ToDictionary(t => t, t => Math.Log(1 + (double)total / Math.Max(1, indices.Count(i => i.ContainsKey(t)))));

        // Para responder, o tópico precisa casar com pelo menos um termo "forte" (título, pergunta de exemplo ou
        // palavra-chave) e com mais de uma palavra da pergunta, a não ser que a pergunta tenha uma palavra só. Assim
        // uma palavra solta ("quem", "tempo") não leva a uma resposta sem relação.
        return topicos
            .Select((topico, i) =>
            {
                var casados = termos.Where(t => indices[i].ContainsKey(t)).ToList();
                var forte = casados.Any(t => indices[i][t] >= 2);
                var suficiente = forte && (casados.Count >= 2 || casados.Count == termos.Count);
                return (Topico: topico, Pontos: suficiente ? casados.Sum(t => indices[i][t] * idf[t]) : 0);
            })
            .Where(x => x.Pontos >= PontuacaoMinima)
            .OrderByDescending(x => x.Pontos)
            .ToList();
    }

    public static bool EhCumprimento(string? pergunta)
    {
        var texto = Normalizar(pergunta);
        return texto.Length > 0 && Termos(pergunta).Count == 0
               && texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(p => p is "oi" or "ola" or "bom" or "boa" or "elson");
    }

    public static bool EhAgradecimento(string? pergunta)
    {
        var texto = Normalizar(pergunta);
        return texto.Contains("obrigad", StringComparison.Ordinal) || texto.Contains("valeu", StringComparison.Ordinal)
               || texto.Contains("agradec", StringComparison.Ordinal);
    }

    // Peso de cada radical no tópico: título e palavras-chave 3, perguntas de exemplo 2, texto da resposta 1.
    private static Dictionary<string, double> Indexar(AssistenteTopico topico)
    {
        var pesos = new Dictionary<string, double>(StringComparer.Ordinal);
        void Somar(IEnumerable<string?>? textos, double peso)
        {
            foreach (var termo in (textos ?? Array.Empty<string?>()).SelectMany(Termos))
            {
                pesos[termo] = Math.Max(pesos.GetValueOrDefault(termo), peso);
            }
        }

        Somar(new[] { topico.Resposta, topico.Observacao }.Concat(topico.Passos ?? Array.Empty<string>()), 1);
        Somar(topico.Perguntas, 2);
        Somar(new[] { topico.Titulo }.Concat(topico.PalavrasChave ?? Array.Empty<string>()), 3);
        return pesos;
    }

    public static IReadOnlyList<string> Termos(string? texto) =>
        Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => Sinonimos.GetValueOrDefault(p, p))
            .Where(p => p.Length > 1 && !PalavrasVazias.Contains(p))
            .Select(Radical)
            .ToList();

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var semAcento = new StringBuilder();
        foreach (var c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            semAcento.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }

        return string.Join(' ', semAcento.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Radical(string palavra)
    {
        foreach (var sufixo in Sufixos)
        {
            if (palavra.Length - sufixo.Length >= 3 && palavra.EndsWith(sufixo, StringComparison.Ordinal))
            {
                return palavra[..^sufixo.Length];
            }
        }

        return palavra;
    }
}

public interface IAssistenteService
{
    AssistenteBoasVindasDto BoasVindas(string primeiroNome, string perfil);

    AssistenteRespostaDto Responder(string? pergunta, string? topicoId, string perfil);
}

public sealed class AssistenteService : IAssistenteService
{
    private const int MaximoSugestoes = 3;
    private readonly ICurrentUserService _currentUserService;
    private readonly string _nome;

    public AssistenteService(ICurrentUserService currentUserService, string nomeDoAssistente)
    {
        _currentUserService = currentUserService;
        _nome = nomeDoAssistente;
    }

    private static IReadOnlyList<AssistenteTopico> Topicos => AssistenteBaseConhecimento.Topicos;

    public AssistenteBoasVindasDto BoasVindas(string primeiroNome, string perfil)
    {
        var saudacao = string.IsNullOrWhiteSpace(primeiroNome)
            ? $"Olá! Me chamo {_nome}, sou seu assistente."
            : $"Olá, {primeiroNome}! Me chamo {_nome}, sou seu assistente.";
        return new AssistenteBoasVindasDto(saudacao, perfil, Frequentes());
    }

    public AssistenteRespostaDto Responder(string? pergunta, string? topicoId, string perfil)
    {
        var escolhido = string.IsNullOrWhiteSpace(topicoId) ? null : Topicos.FirstOrDefault(t => t.Id == topicoId);
        if (escolhido is not null)
        {
            return Montar(escolhido, perfil, Array.Empty<AssistenteSugestaoDto>());
        }

        if (AssistenteBusca.EhAgradecimento(pergunta))
        {
            return Simples("Por nada! Se precisar de mais alguma coisa, é só perguntar.", Array.Empty<AssistenteSugestaoDto>());
        }

        if (AssistenteBusca.EhCumprimento(pergunta))
        {
            return Simples("Olá! Me diga o que você quer fazer no sistema, por exemplo: \"como fecho o trimestre?\".", Frequentes());
        }

        var encontrados = AssistenteBusca.Buscar(pergunta, Topicos);
        if (encontrados.Count == 0)
        {
            return Simples(
                "Não encontrei isso no manual. Tente com outras palavras (por exemplo, \"como lanço notas\") ou veja algumas sugestões:",
                Frequentes(),
                new AssistenteLink("/Manual?handler=DownloadPdf", "Baixar manual (PDF)"));
        }

        var melhor = encontrados[0];
        var parecidos = encontrados
            .Skip(1)
            .Where(x => x.Pontos >= melhor.Pontos * 0.6)
            .Take(MaximoSugestoes)
            .Select(x => new AssistenteSugestaoDto(x.Topico.Id, x.Topico.Titulo))
            .ToList();
        return Montar(melhor.Topico, perfil, parecidos);
    }

    private AssistenteRespostaDto Montar(AssistenteTopico topico, string perfil, IReadOnlyList<AssistenteSugestaoDto> sugestoes)
    {
        var exigidas = topico.Permissoes ?? Array.Empty<string>();
        var temPermissao = exigidas.Count == 0 || exigidas.Any(_currentUserService.TemPermissao);
        var aviso = temPermissao
            ? null
            : $"Seu perfil ({perfil}) não tem a permissão \"{Permissoes.Descrever(exigidas[0])}\". " +
              "Se precisar, peça a quem gerencia os perfis na escola.";

        return new AssistenteRespostaDto(
            true,
            topico.Id,
            topico.Resposta,
            topico.Passos ?? Array.Empty<string>(),
            topico.Observacao,
            aviso,
            temPermissao ? topico.Link : null,
            topico.SecaoManual,
            sugestoes);
    }

    private static AssistenteRespostaDto Simples(string texto, IReadOnlyList<AssistenteSugestaoDto> sugestoes, AssistenteLink? link = null) =>
        new(false, null, texto, Array.Empty<string>(), null, null, link, null, sugestoes);

    // Perguntas frequentes da tela inicial: primeiro as que o perfil consegue fazer.
    private IReadOnlyList<AssistenteSugestaoDto> Frequentes() =>
        Topicos
            .Where(t => t.Frequente)
            .OrderByDescending(t => t.Permissoes is null || t.Permissoes.Count == 0 || t.Permissoes.Any(_currentUserService.TemPermissao))
            .Take(6)
            .Select(t => new AssistenteSugestaoDto(t.Id, t.Titulo))
            .ToList();
}
