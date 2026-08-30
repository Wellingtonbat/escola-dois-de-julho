using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Web.Pages.Turmas;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly ITurmaService _turmaService;
    private readonly ISerieService _serieService;
    private readonly IProfessorService _professorService;

    public IndexModel(ITurmaService turmaService, ISerieService serieService, IProfessorService professorService)
    {
        _turmaService = turmaService;
        _serieService = serieService;
        _professorService = professorService;
    }

    public IReadOnlyList<TurmaListItemDto> Turmas { get; private set; } = Array.Empty<TurmaListItemDto>();
    public IReadOnlyList<SelectListItem> Series { get; private set; } = Array.Empty<SelectListItem>();

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public Guid? SerieId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? AnoLetivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = PageSizeFixo;

    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (IsProfessorOnly())
        {
            TempData["ErrorMessage"] = "Seu perfil não tem acesso a esta tela.";
            return RedirectToPage("/Notas/Index");
        }

        await LoadSeriesAsync(cancellationToken);

        bool? statusFilter = Status switch
        {
            "ativas" => true,
            "inativas" => false,
            _ => null
        };

        var filter = new TurmaListFilter(Busca, SerieId, AnoLetivo, statusFilter);
        var turmasFiltradas = await _turmaService.ListarAsync(filter, cancellationToken);

        if (IsProfessorOnly())
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken);
            if (escopo is not null)
            {
                turmasFiltradas = turmasFiltradas
                    .Where(x => escopo.TurmaIds.Contains(x.Id))
                    .ToList();
            }
        }

        TotalCount = turmasFiltradas.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Turmas = turmasFiltradas
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        string nome,
        Guid serieId,
        string turno,
        int anoLetivo,
        bool isAtiva,
        string? busca,
        string? status,
        Guid? serieFilter,
        int? anoLetivoFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageTurmas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar turmas.";
            return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieFilter, AnoLetivo = anoLetivoFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _turmaService.AtualizarAsync(
            id,
            new TurmaCreateRequest(nome, serieId, turno, anoLetivo, isAtiva),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível atualizar a turma.";
            return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieFilter, AnoLetivo = anoLetivoFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Turma atualizada com sucesso.";
        return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieFilter, AnoLetivo = anoLetivoFilter, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(
        Guid id,
        string? busca,
        string? status,
        Guid? serieId,
        int? anoLetivo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageTurmas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar status de turmas.";
            return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieId, AnoLetivo = anoLetivo, PageNumber = pageNumber, PageSize = pageSize });
        }

        var updated = await _turmaService.AlternarStatusAsync(id, cancellationToken);
        if (!updated)
        {
            TempData["ErrorMessage"] = "Turma não encontrada para atualização de status.";
            return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieId, AnoLetivo = anoLetivo, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Status da turma atualizado com sucesso.";
        return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieId, AnoLetivo = anoLetivo, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid id,
        string? busca,
        string? status,
        Guid? serieId,
        int? anoLetivo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageTurmas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir turmas.";
            return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieId, AnoLetivo = anoLetivo, PageNumber = pageNumber, PageSize = pageSize });
        }

        var deleted = await _turmaService.ExcluirAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Turma não encontrada para exclusão.";
            return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieId, AnoLetivo = anoLetivo, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Turma excluída com sucesso.";
        return RedirectToPage("/Turmas/Index", new { Busca = busca, Status = status, SerieId = serieId, AnoLetivo = anoLetivo, PageNumber = pageNumber, PageSize = pageSize });
    }

    private bool CanManageTurmas()
    {
        return User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");
    }

    private bool IsProfessorOnly() => User.IsInRole("Professor") && !CanManageTurmas();

    private async Task LoadSeriesAsync(CancellationToken cancellationToken)
    {
        var series = await _serieService.ListarAsync(new SerieListFilter(null, true), cancellationToken);
        Series = series
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.Id.ToString()))
            .ToList();
    }
}
