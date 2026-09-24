namespace SistemaEscolar.Application.Abstractions;

// Nomes dos perfis (roles do Identity) usados em todo o sistema. Os perfis não são exclusivos:
// um mesmo usuário pode acumular vários (ex.: Professor + ViceDiretor).
public static class Perfis
{
    public const string Diretor = "Diretor";
    public const string ViceDiretor = "ViceDiretor";
    public const string Coordenador = "Coordenador";

    // Grafia antiga do perfil de Coordenador, ainda aceita para não quebrar contas já existentes.
    public const string CoordenadorLegado = "Cordenador";

    public const string Secretaria = "Secretaria";
    public const string Professor = "Professor";

    public static string Descrever(string perfil) => perfil switch
    {
        ViceDiretor => "Vice-Diretor",
        Secretaria => "Secretária",
        _ => perfil
    };
}
