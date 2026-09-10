namespace SistemaEscolar.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardDadosDto> ObterDadosAsync(DashboardFiltroDto filtro, CancellationToken cancellationToken = default);
}
