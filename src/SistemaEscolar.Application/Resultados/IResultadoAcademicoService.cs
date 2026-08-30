namespace SistemaEscolar.Application.Resultados;

public interface IResultadoAcademicoService
{
    Task<IReadOnlyList<ResultadoAcademicoDto>> ListarAsync(ResultadoAcademicoFilter filter, CancellationToken cancellationToken = default);
}