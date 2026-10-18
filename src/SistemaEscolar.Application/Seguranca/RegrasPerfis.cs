using SistemaEscolar.Application.Abstractions;

namespace SistemaEscolar.Application.Seguranca;

// Regras da tela "Perfis e Permissões", sem acesso a banco (testáveis isoladamente). Todas partem do mesmo
// princípio: quem gerencia perfis só concede, altera ou remove o que também tem.
public static class RegrasPerfis
{
    public const int TamanhoMaximoNome = 40;

    // Perfis que já vêm com o sistema. O código depende do nome deles (contas de professor, marcador de
    // Vice-Diretor, conta inicial do Diretor), então não podem ser renomeados nem excluídos.
    private static readonly HashSet<string> PerfisPadrao = new(StringComparer.OrdinalIgnoreCase)
    {
        Perfis.Diretor, Perfis.ViceDiretor, Perfis.Coordenador,
        Perfis.CoordenadorLegado, Perfis.Secretaria, Perfis.Professor
    };

    public static bool EhPadrao(string nome) => PerfisPadrao.Contains(nome);

    // Diretor e Vice-Diretor ficam sempre com todas as permissões (acesso total) e não são editáveis: a escola
    // nunca perde o acesso total, e permissões criadas em versões futuras chegam a eles automaticamente.
    public static bool EhSomenteLeitura(string nome) =>
        string.Equals(nome, Perfis.Diretor, StringComparison.OrdinalIgnoreCase)
        || string.Equals(nome, Perfis.ViceDiretor, StringComparison.OrdinalIgnoreCase);

    // Ordem na lista: perfis padrão primeiro, na hierarquia da escola; depois os criados, em ordem alfabética.
    public static int Ordem(string nome) => nome switch
    {
        Perfis.Diretor => 0,
        Perfis.ViceDiretor => 1,
        Perfis.Coordenador => 2,
        Perfis.CoordenadorLegado => 3,
        Perfis.Secretaria => 4,
        Perfis.Professor => 5,
        _ => 10
    };

    public static bool PodeEditar(Func<string, bool> temPermissao, string nome, IEnumerable<string> permissoesAtuais) =>
        PermissoesPerfil.PodeGerenciarPerfis(temPermissao)
        && !EhSomenteLeitura(nome)
        && PermissoesPerfil.CobrePermissoes(temPermissao, permissoesAtuais);

    public static bool PodeExcluir(Func<string, bool> temPermissao, string nome, IEnumerable<string> permissoesAtuais, int totalUsuarios) =>
        PodeEditar(temPermissao, nome, permissoesAtuais) && !EhPadrao(nome) && totalUsuarios == 0;

    public static string? ValidarNome(string? nome, Guid? idAtual, IEnumerable<(Guid Id, string Nome)> existentes)
    {
        var limpo = (nome ?? string.Empty).Trim();
        if (limpo.Length == 0)
        {
            return "Informe o nome do perfil.";
        }

        if (limpo.Length > TamanhoMaximoNome)
        {
            return $"O nome do perfil deve ter no máximo {TamanhoMaximoNome} caracteres.";
        }

        // Compara também com o nome exibido ("Vice-Diretor", "Secretária") para não haver dois perfis que
        // aparecem iguais na tela.
        var repetido = existentes.Any(x => x.Id != idAtual
            && (string.Equals(x.Nome, limpo, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Perfis.Descrever(x.Nome), limpo, StringComparison.OrdinalIgnoreCase)));
        return repetido ? "Já existe um perfil com este nome." : null;
    }

    public static string? ValidarCriacao(Func<string, bool> temPermissao, IReadOnlyCollection<string> novas)
    {
        if (!PermissoesPerfil.PodeGerenciarPerfis(temPermissao))
        {
            return MensagemSemPermissao;
        }

        return ValidarPermissoesConcedidas(temPermissao, novas);
    }

    public static string? ValidarAlteracao(
        Func<string, bool> temPermissao,
        string nome,
        IReadOnlyCollection<string> atuais,
        IReadOnlyCollection<string> novas,
        bool usuarioPertenceAoPerfil)
    {
        if (!PermissoesPerfil.PodeGerenciarPerfis(temPermissao))
        {
            return MensagemSemPermissao;
        }

        if (EhSomenteLeitura(nome))
        {
            return "Os perfis Diretor e Vice-Diretor têm sempre todas as permissões e não podem ser alterados.";
        }

        if (!PermissoesPerfil.CobrePermissoes(temPermissao, atuais))
        {
            return "Este perfil tem permissões que você não tem; por isso, só pode ser alterado por quem tem todas elas.";
        }

        if (usuarioPertenceAoPerfil && atuais.Contains(Permissoes.PerfisGerenciar) && !novas.Contains(Permissoes.PerfisGerenciar))
        {
            return "Você não pode retirar a permissão \"Gerenciar perfis e permissões\" de um perfil que você mesmo usa.";
        }

        // Permissões que o perfil já tinha não precisam ser revalidadas (o usuário cobre todas, ver acima).
        return ValidarPermissoesConcedidas(temPermissao, novas.Except(atuais).ToList());
    }

    public static string? ValidarExclusao(Func<string, bool> temPermissao, string nome, IReadOnlyCollection<string> atuais, int totalUsuarios)
    {
        if (!PermissoesPerfil.PodeGerenciarPerfis(temPermissao))
        {
            return MensagemSemPermissao;
        }

        if (EhPadrao(nome))
        {
            return "Os perfis que vêm com o sistema não podem ser excluídos.";
        }

        if (!PermissoesPerfil.CobrePermissoes(temPermissao, atuais))
        {
            return "Este perfil tem permissões que você não tem; por isso, só pode ser excluído por quem tem todas elas.";
        }

        return totalUsuarios > 0
            ? $"Este perfil está em uso por {totalUsuarios} usuário(s). Troque o perfil dessas contas em Usuários antes de excluí-lo."
            : null;
    }

    private const string MensagemSemPermissao = "Seu perfil não tem permissão para gerenciar perfis.";

    private static string? ValidarPermissoesConcedidas(Func<string, bool> temPermissao, IReadOnlyCollection<string> concedidas)
    {
        var desconhecida = concedidas.FirstOrDefault(x => !Permissoes.Existe(x));
        if (desconhecida is not null)
        {
            return "Permissão desconhecida.";
        }

        var semDireito = concedidas.Where(x => !temPermissao(x)).Select(Permissoes.Descrever).ToList();
        return semDireito.Count == 0
            ? null
            : $"Você só pode conceder permissões que também tem. Sem direito a conceder: {string.Join(", ", semDireito)}.";
    }
}
