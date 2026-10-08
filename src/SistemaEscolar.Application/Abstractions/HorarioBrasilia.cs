namespace SistemaEscolar.Application.Abstractions;

// Horário de Brasília (UTC-3, sem horário de verão desde 2019). O servidor roda em UTC, então toda
// regra que depende do "dia de hoje" (janela dos períodos de lançamento, ano letivo padrão, datas
// impressas nos PDFs) deve usar este relógio — senão, a partir das 21h, o sistema já se comporta
// como se fosse o dia seguinte. Usa deslocamento fixo para não depender do nome do fuso no servidor.
public static class HorarioBrasilia
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

    public static DateTime Agora => DeUtc(DateTime.UtcNow);

    public static DateTime Hoje => Agora.Date;

    public static DateTime DeUtc(DateTime utc) => utc + Offset;

    public static DateTime ParaUtc(DateTime horarioBrasilia) => horarioBrasilia - Offset;
}
