namespace SistemaEscolar.Application.Resultados;

public interface IResultadoAcademicoRepository
{
    Task<IReadOnlyList<ResultadoAcademicoDto>> ListarAsync(ResultadoAcademicoFilter filter, CancellationToken cancellationToken = default);
}