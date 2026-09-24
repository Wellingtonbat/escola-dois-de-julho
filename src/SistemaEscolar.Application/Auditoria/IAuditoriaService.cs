namespace SistemaEscolar.Application.Auditoria;

public interface IAuditoriaService
{
    // Tabelas que podem ser filtradas na tela, com o nome amigável de cada uma.
    IReadOnlyList<AuditoriaEntidadeDto> Entidades { get; }

    Task<AuditoriaListResult> ListarAsync(AuditoriaFiltro filtro, CancellationToken cancellationToken = default);
}
