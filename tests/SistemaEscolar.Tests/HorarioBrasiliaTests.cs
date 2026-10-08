using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Periodos;
using Xunit;

namespace SistemaEscolar.Tests;

public sealed class HorarioBrasiliaTests
{
    private static PeriodoListItemDto Periodo(DateTime dataInicial, DateTime dataFinal) =>
        new(Guid.NewGuid(), 2026, 3, "3º Trimestre", dataInicial, dataFinal, IsAberto: true, AbertoManualmente: false);

    [Fact]
    public void DeUtc_AposAs21hDeBrasilia_ContinuaNoMesmoDia()
    {
        // 31/10 00:30 UTC = 30/10 21:30 em Brasília.
        var brasilia = HorarioBrasilia.DeUtc(new DateTime(2026, 10, 31, 0, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 10, 30), brasilia.Date);
        Assert.Equal(21, brasilia.Hour);
    }

    [Fact]
    public void ParaUtc_DesfazDeUtc()
    {
        var utc = new DateTime(2026, 10, 31, 0, 30, 0, DateTimeKind.Utc);

        Assert.Equal(utc, HorarioBrasilia.ParaUtc(HorarioBrasilia.DeUtc(utc)));
    }

    [Fact]
    public void ParaUtc_DataSemFusoVindaDaUrl_SaiComoUtc()
    {
        // "De=2026-09-08" na URL chega como Kind=Unspecified; o Npgsql só aceita UTC em timestamptz.
        var deFiltro = new DateTime(2026, 9, 8);

        var utc = HorarioBrasilia.ParaUtc(deFiltro);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 9, 8, 3, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void Periodo_ContinuaAbertoNaNoiteDoUltimoDia()
    {
        var periodo = Periodo(new DateTime(2026, 9, 1), new DateTime(2026, 10, 30));
        var ultimoDiaAs22h = HorarioBrasilia.DeUtc(new DateTime(2026, 10, 31, 1, 0, 0, DateTimeKind.Utc));

        Assert.True(PeriodoDisponibilidade.EstaAberto(periodo, ultimoDiaAs22h));
    }

    [Fact]
    public void Periodo_NaoAbreNaNoiteDaVespera()
    {
        var periodo = Periodo(new DateTime(2026, 9, 1), new DateTime(2026, 10, 30));
        var vesperaAs22h = HorarioBrasilia.DeUtc(new DateTime(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc));

        Assert.False(PeriodoDisponibilidade.EstaAberto(periodo, vesperaAs22h));
    }
}
