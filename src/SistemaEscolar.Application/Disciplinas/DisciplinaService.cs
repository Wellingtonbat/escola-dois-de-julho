using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Application.Disciplinas;

public sealed class DisciplinaService : IDisciplinaService
{
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IDisciplinaSerieRepository _disciplinaSerieRepository;
    private readonly ISerieRepository _serieRepository;

    public DisciplinaService(
        IDisciplinaRepository disciplinaRepository,
        IDisciplinaSerieRepository disciplinaSerieRepository,
        ISerieRepository serieRepository)
    {
        _disciplinaRepository = disciplinaRepository;
        _disciplinaSerieRepository = disciplinaSerieRepository;
        _serieRepository = serieRepository;
    }

    public async Task<IReadOnlyList<DisciplinaListItemDto>> ListarAsync(DisciplinaListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var disciplinas = await _disciplinaRepository.GetAllAsync(filter, cancellationToken);
        var todosVinculos = await _disciplinaSerieRepository.GetAllAsync(cancellationToken);
        var series = await _serieRepository.GetAllAsync(null, cancellationToken);

        return disciplinas
            .OrderBy(x => x.Nome)
            .Select(disciplina =>
            {
                var seriesDto = todosVinculos
                    .Where(v => v.DisciplinaId == disciplina.Id)
                    .Select(v => MontarSerieDto(v.SerieId, series))
                    .ToList();

                return new DisciplinaListItemDto(disciplina.Id, disciplina.Nome, disciplina.Codigo, seriesDto, disciplina.CargaHoraria, disciplina.IsAtiva);
            })
            .ToList();
    }

    public async Task<DisciplinaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var disciplina = await _disciplinaRepository.GetByIdAsync(id, cancellationToken);
        if (disciplina is null)
        {
            return null;
        }

        var serieIds = await _disciplinaSerieRepository.GetSerieIdsByDisciplinaIdAsync(id, cancellationToken);
        var series = await _serieRepository.GetAllAsync(null, cancellationToken);
        var seriesDto = serieIds.Select(serieId => MontarSerieDto(serieId, series)).ToList();

        return new DisciplinaListItemDto(disciplina.Id, disciplina.Nome, disciplina.Codigo, seriesDto, disciplina.CargaHoraria, disciplina.IsAtiva);
    }

    public async Task<DisciplinaCreateResult> CriarAsync(DisciplinaCreateRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (!validation.Result.Succeeded)
        {
            return validation.Result;
        }

        var disciplina = new Domain.Entities.Disciplina
        {
            Id = Guid.NewGuid(),
            Nome = validation.Nome,
            Codigo = validation.Codigo,
            CargaHoraria = request.CargaHoraria,
            IsAtiva = request.IsAtiva
        };

        await _disciplinaRepository.AddAsync(disciplina, cancellationToken);
        await _disciplinaSerieRepository.SubstituirAsync(disciplina.Id, validation.SerieIdsValidos, cancellationToken);

        return DisciplinaCreateResult.Success();
    }

    public async Task<DisciplinaCreateResult> AtualizarAsync(Guid id, DisciplinaCreateRequest request, CancellationToken cancellationToken = default)
    {
        var disciplina = await _disciplinaRepository.GetByIdAsync(id, cancellationToken);
        if (disciplina is null)
        {
            return DisciplinaCreateResult.Fail("Disciplina não encontrada.");
        }

        var validation = await ValidateAsync(request, id, cancellationToken);
        if (!validation.Result.Succeeded)
        {
            return validation.Result;
        }

        disciplina.Nome = validation.Nome;
        disciplina.Codigo = validation.Codigo;
        disciplina.CargaHoraria = request.CargaHoraria;
        disciplina.IsAtiva = request.IsAtiva;

        await _disciplinaRepository.UpdateAsync(disciplina, cancellationToken);
        await _disciplinaSerieRepository.SubstituirAsync(disciplina.Id, validation.SerieIdsValidos, cancellationToken);

        return DisciplinaCreateResult.Success();
    }

    public async Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var disciplina = await _disciplinaRepository.GetByIdAsync(id, cancellationToken);
        if (disciplina is null)
        {
            return false;
        }

        disciplina.IsAtiva = !disciplina.IsAtiva;
        await _disciplinaRepository.UpdateAsync(disciplina, cancellationToken);

        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var disciplina = await _disciplinaRepository.GetByIdAsync(id, cancellationToken);
        if (disciplina is null)
        {
            return false;
        }

        await _disciplinaRepository.SoftDeleteAsync(disciplina, cancellationToken);
        return true;
    }

    private async Task<(DisciplinaCreateResult Result, string Nome, string Codigo, List<Guid> SerieIdsValidos)> ValidateAsync(
        DisciplinaCreateRequest request,
        Guid? ignoreId,
        CancellationToken cancellationToken)
    {
        var nome = (request.Nome ?? string.Empty).Trim();
        var codigo = (request.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        var serieIdsVazios = new List<Guid>();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return (DisciplinaCreateResult.Fail("O nome da disciplina é obrigatório."), nome, codigo, serieIdsVazios);
        }

        if (string.IsNullOrWhiteSpace(codigo))
        {
            return (DisciplinaCreateResult.Fail("O código da disciplina é obrigatório."), nome, codigo, serieIdsVazios);
        }

        if (request.CargaHoraria <= 0)
        {
            return (DisciplinaCreateResult.Fail("A carga horária deve ser maior que zero."), nome, codigo, serieIdsVazios);
        }

        var serieIds = (request.SerieIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (serieIds.Count == 0)
        {
            return (DisciplinaCreateResult.Fail("Selecione ao menos uma série para a disciplina."), nome, codigo, serieIdsVazios);
        }

        if (await _disciplinaRepository.CodigoExisteAsync(codigo, ignoreId, cancellationToken))
        {
            return (DisciplinaCreateResult.Fail("Já existe disciplina cadastrada com este código."), nome, codigo, serieIdsVazios);
        }

        foreach (var serieId in serieIds)
        {
            if (await _serieRepository.GetByIdAsync(serieId, cancellationToken) is null)
            {
                return (DisciplinaCreateResult.Fail("Uma das séries selecionadas não foi encontrada."), nome, codigo, serieIdsVazios);
            }
        }

        return (DisciplinaCreateResult.Success(), nome, codigo, serieIds);
    }

    private static DisciplinaSerieDto MontarSerieDto(Guid serieId, IReadOnlyList<Domain.Entities.Serie> series)
    {
        var serie = series.FirstOrDefault(x => x.Id == serieId);
        return new DisciplinaSerieDto(serieId, serie?.Nome ?? "Série não encontrada");
    }
}
