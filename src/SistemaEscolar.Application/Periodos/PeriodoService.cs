using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Notas;

namespace SistemaEscolar.Application.Periodos;

public sealed class PeriodoService : IPeriodoService
{
    private readonly IPeriodoRepository _periodoRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotaRepository _notaRepository;

    public PeriodoService(
        IPeriodoRepository periodoRepository,
        ICurrentUserService currentUserService,
        INotaRepository notaRepository)
    {
        _periodoRepository = periodoRepository;
        _currentUserService = currentUserService;
        _notaRepository = notaRepository;
    }

    public async Task<IReadOnlyList<PeriodoListItemDto>> ListarAsync(PeriodoListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        // O filtro de Status (aberto/fechado) reflete a disponibilidade EFETIVA (data + flag manual),
        // não apenas o campo IsAberto isolado — por isso é aplicado aqui, em memória, e não no repositório.
        var repositoryFilter = filter is null ? null : new PeriodoListFilter(filter.AnoLetivo, filter.Trimestre, null);
        var periodos = await _periodoRepository.GetAllAsync(repositoryFilter, cancellationToken);
        var hoje = DateTime.UtcNow;

        var dtos = periodos
            .OrderByDescending(x => x.AnoLetivo)
            .ThenBy(x => x.Bimestre)
            .Select(Map);

        if (filter?.IsAberto.HasValue == true)
        {
            dtos = dtos.Where(x => PeriodoDisponibilidade.EstaAberto(x, hoje) == filter.IsAberto.Value);
        }

        return dtos.ToList();
    }

    public async Task<PeriodoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var periodo = await _periodoRepository.GetByIdAsync(id, cancellationToken);
        return periodo is null ? null : Map(periodo);
    }

    public async Task<PeriodoListItemDto?> ObterPorAnoETrimestreAsync(int anoLetivo, int trimestre, CancellationToken cancellationToken = default)
    {
        var periodo = await _periodoRepository.GetByAnoETrimestreAsync(anoLetivo, trimestre, cancellationToken);
        return periodo is null ? null : Map(periodo);
    }

    public async Task<PeriodoCreateResult> CriarAsync(PeriodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var periodo = new Domain.Entities.PeriodoLancamento
        {
            Id = Guid.NewGuid(),
            AnoLetivo = request.AnoLetivo,
            Bimestre = request.Trimestre,
            Descricao = request.Descricao.Trim(),
            DataInicial = request.DataInicial.Date,
            DataFinal = request.DataFinal.Date,
            IsAberto = true
        };

        await _periodoRepository.AddAsync(periodo, cancellationToken);
        return PeriodoCreateResult.Success();
    }

    public async Task<PeriodoCreateResult> AtualizarAsync(Guid id, PeriodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        var periodo = await _periodoRepository.GetByIdAsync(id, cancellationToken);
        if (periodo is null)
        {
            return PeriodoCreateResult.Fail("Período não encontrado.");
        }

        var validation = await ValidateAsync(request, id, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        periodo.AnoLetivo = request.AnoLetivo;
        periodo.Bimestre = request.Trimestre;
        periodo.Descricao = request.Descricao.Trim();
        periodo.DataInicial = request.DataInicial.Date;
        periodo.DataFinal = request.DataFinal.Date;

        await _periodoRepository.UpdateAsync(periodo, cancellationToken);
        return PeriodoCreateResult.Success();
    }

    public async Task<PeriodoCreateResult> AbrirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole("Diretor"))
        {
            return PeriodoCreateResult.Fail("Somente diretores podem abrir períodos.");
        }

        var periodo = await _periodoRepository.GetByIdAsync(id, cancellationToken);
        if (periodo is null)
        {
            return PeriodoCreateResult.Fail("Período não encontrado.");
        }

        periodo.IsAberto = true;
        await _periodoRepository.UpdateAsync(periodo, cancellationToken);
        return PeriodoCreateResult.Success();
    }

    public async Task<PeriodoCreateResult> FecharAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole("Diretor"))
        {
            return PeriodoCreateResult.Fail("Somente diretores podem fechar períodos.");
        }

        var periodo = await _periodoRepository.GetByIdAsync(id, cancellationToken);
        if (periodo is null)
        {
            return PeriodoCreateResult.Fail("Período não encontrado.");
        }

        if (await _notaRepository.TemPendenciasAsync(periodo.Id, cancellationToken))
        {
            return PeriodoCreateResult.Fail("Não é possível fechar o período com lançamentos pendentes.");
        }

        periodo.IsAberto = false;
        await _periodoRepository.UpdateAsync(periodo, cancellationToken);
        return PeriodoCreateResult.Success();
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var periodo = await _periodoRepository.GetByIdAsync(id, cancellationToken);
        if (periodo is null)
        {
            return false;
        }

        await _periodoRepository.SoftDeleteAsync(periodo, cancellationToken);
        return true;
    }

    private async Task<PeriodoCreateResult> ValidateAsync(PeriodoCreateRequest request, Guid? ignoreId, CancellationToken cancellationToken)
    {
        if (request.AnoLetivo < 2000 || request.AnoLetivo > 2100)
        {
            return PeriodoCreateResult.Fail("Informe um ano letivo válido.");
        }

        if (request.Trimestre < 1 || request.Trimestre > 3)
        {
            return PeriodoCreateResult.Fail("O trimestre deve estar entre 1 e 3.");
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            return PeriodoCreateResult.Fail("A descrição do período é obrigatória.");
        }

        if (request.DataInicial.Date > request.DataFinal.Date)
        {
            return PeriodoCreateResult.Fail("A data inicial não pode ser maior que a data final.");
        }

        if (await _periodoRepository.ExisteAnoETrimestreAsync(request.AnoLetivo, request.Trimestre, ignoreId, cancellationToken))
        {
            return PeriodoCreateResult.Fail("Já existe período cadastrado para este ano letivo e trimestre.");
        }

        if (await _periodoRepository.ExisteSobreposicaoAsync(request.DataInicial, request.DataFinal, ignoreId, cancellationToken))
        {
            return PeriodoCreateResult.Fail("Já existe período com sobreposição de datas.");
        }

        return PeriodoCreateResult.Success();
    }

    private static PeriodoListItemDto Map(Domain.Entities.PeriodoLancamento periodo)
    {
        return new PeriodoListItemDto(
            periodo.Id,
            periodo.AnoLetivo,
            periodo.Bimestre,
            periodo.Descricao,
            periodo.DataInicial,
            periodo.DataFinal,
            periodo.IsAberto);
    }
}
