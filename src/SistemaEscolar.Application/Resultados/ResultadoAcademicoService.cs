namespace SistemaEscolar.Application.Resultados;

public sealed class ResultadoAcademicoService : IResultadoAcademicoService
{
    private readonly IResultadoAcademicoRepository _resultadoRepository;

    public ResultadoAcademicoService(IResultadoAcademicoRepository resultadoRepository)
    {
        _resultadoRepository = resultadoRepository;
    }

    public Task<IReadOnlyList<ResultadoAcademicoDto>> ListarAsync(ResultadoAcademicoFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.AnoLetivo < 2000 || filter.AnoLetivo > 2100)
        {
            throw new ArgumentException("Ano letivo inválido para consulta de resultados.", nameof(filter));
        }

        return _resultadoRepository.ListarAsync(filter, cancellationToken);
    }
}