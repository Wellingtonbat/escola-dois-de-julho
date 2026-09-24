namespace SistemaEscolar.Application.Abstractions;

public static class AppClaimTypes
{
    // Nome completo do usuário, gravado no login para que a auditoria registre "quem" sem consultar a conta.
    public const string FullName = "sistemaescolar:full_name";
}
