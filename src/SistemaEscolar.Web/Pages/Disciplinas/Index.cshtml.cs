using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Web.Pages.Disciplinas;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly IDisciplinaService _disciplinaService;
    private readonly ISerieService _serieService;
    private readonly IProfessorService _professorService;

    public IndexModel(IDisciplinaService disciplinaService, ISerieService serieService, IProfessorService professorService)
    {
        _disciplinaService = disciplinaService;
        _serieService = serieService;
        _professorService = professorService;
    }

    public IReadOnlyList<DisciplinaListItemDto> Disciplinas { get; private set; } = Array.Empty<DisciplinaListItemDto>();
    public IReadOnlyList<SelectListItem> Series { get; private set; } = Array.Empty<SelectListItem>();

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

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

        var filter = new DisciplinaListFilter(Busca, statusFilter);
        var disciplinasFiltradas = await _disciplinaService.ListarAsync(filter, cancellationToken);

        if (IsProfessorOnly())
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken);
            if (escopo is not null)
            {
                disciplinasFiltradas = disciplinasFiltradas
                    .Where(x => escopo.DisciplinaIds.Contains(x.Id))
                    .ToList();
            }
        }

        TotalCount = disciplinasFiltradas.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Disciplinas = disciplinasFiltradas
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        string nome,
        string codigo,
        List<Guid> serieIds,
        int cargaHoraria,
        bool isAtiva,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageDisciplinas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar disciplinas.";
            return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _disciplinaService.AtualizarAsync(
            id,
            new DisciplinaCreateRequest(nome, codigo, serieIds ?? new List<Guid>(), cargaHoraria, isAtiva),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível atualizar a disciplina.";
            return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Disciplina atualizada com sucesso.";
        return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(
        Guid id,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageDisciplinas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar status de disciplinas.";
            return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var updated = await _disciplinaService.AlternarStatusAsync(id, cancellationToken);
        if (!updated)
        {
            TempData["ErrorMessage"] = "Disciplina não encontrada para atualização de status.";
            return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Status da disciplina atualizado com sucesso.";
        return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid id,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageDisciplinas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir disciplinas.";
            return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var deleted = await _disciplinaService.ExcluirAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Disciplina não encontrada para exclusão.";
            return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Disciplina excluída com sucesso.";
        return RedirectToPage("/Disciplinas/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    private bool CanManageDisciplinas()
    {
        return User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");
    }

    private bool IsProfessorOnly() =>
        User.IsInRole("Professor") && !CanManageDisciplinas();

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
