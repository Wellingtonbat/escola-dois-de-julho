namespace SistemaEscolar.Application.Periodos;

// O lançamento de notas só deve ficar disponível dentro da janela de datas do período
// (DataInicial-DataFinal); o campo IsAberto deixou de significar "aberto" isoladamente e
// passou a representar apenas um fechamento antecipado manual feito pelo Diretor.
public static class PeriodoDisponibilidade
{
    public static bool EstaAberto(PeriodoListItemDto periodo, DateTime hoje)
    {
        return periodo.IsAberto
            && hoje.Date >= periodo.DataInicial.Date
            && hoje.Date <= periodo.DataFinal.Date;
    }

    public static string ObterMotivoFechado(PeriodoListItemDto periodo, DateTime hoje)
    {
        if (!periodo.IsAberto)
        {
            return "Este período foi fechado antecipadamente pelo Diretor.";
        }

        if (hoje.Date < periodo.DataInicial.Date)
        {
            return $"Este período só abre em {periodo.DataInicial:dd/MM/yyyy}.";
        }

        if (hoje.Date > periodo.DataFinal.Date)
        {
            return $"Este período encerrou em {periodo.DataFinal:dd/MM/yyyy}.";
        }

        return "Período fechado.";
    }
}
