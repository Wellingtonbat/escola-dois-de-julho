using SistemaEscolar.Application.Resultados;

namespace SistemaEscolar.Tests.Support;

internal sealed class ResultadoAcademicoServiceStub : IResultadoAcademicoService
{
    private readonly IReadOnlyList<ResultadoAcademicoDto> _items;

    public ResultadoAcademicoServiceStub(IReadOnlyList<ResultadoAcademicoDto> items)
    {
        _items = items;
    }

    public Task<IReadOnlyList<ResultadoAcademicoDto>> ListarAsync(ResultadoAcademicoFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items);
    }
}
