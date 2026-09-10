namespace SistemaEscolar.Application.Periodos;

// Por padrão, o lançamento de notas só fica disponível dentro da janela de datas do período
// (DataInicial-DataFinal). O botão Abrir do Diretor/Coordenador pode forçar uma exceção manual
// (AbertoManualmente) que mantém o período aberto mesmo com a data já vencida — nesse caso, quem
// abriu manualmente é responsável por fechar o período depois; não existe fechamento automático
// enquanto a exceção estiver ligada. O botão Fechar sempre fecha, independente de tudo.
public static class PeriodoDisponibilidade
{
    public static bool EstaAberto(PeriodoListItemDto periodo, DateTime hoje)
    {
        if (!periodo.IsAberto)
        {
            return false;
        }

        return periodo.AbertoManualmente || DentroDaJanela(periodo, hoje);
    }

    public static bool EstaForaDaJanelaPorExcecaoManual(PeriodoListItemDto periodo, DateTime hoje) =>
        periodo.IsAberto && periodo.AbertoManualmente && !DentroDaJanela(periodo, hoje);

    public static string ObterMotivoFechado(PeriodoListItemDto periodo, DateTime hoje)
    {
        if (!periodo.IsAberto)
        {
            return "Este período foi fechado pela Direção/Coordenação.";
        }

        if (hoje.Date < periodo.DataInicial.Date)
        {
            return $"Este período só abre em {periodo.DataInicial:dd/MM/yyyy}.";
        }

        return $"Este período encerrou em {periodo.DataFinal:dd/MM/yyyy}.";
    }

    private static bool DentroDaJanela(PeriodoListItemDto periodo, DateTime hoje) =>
        hoje.Date >= periodo.DataInicial.Date && hoje.Date <= periodo.DataFinal.Date;
}
