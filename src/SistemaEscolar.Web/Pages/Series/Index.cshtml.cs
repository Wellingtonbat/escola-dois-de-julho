using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Web.Pages.Series;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly ISerieService _serieService;
    private readonly IProfessorService _professorService;

    public IndexModel(ISerieService serieService, IProfessorService professorService)
    {
        _serieService = serieService;
        _professorService = professorService;
    }

    public IReadOnlyList<SerieListItemDto> Series { get; private set; } = Array.Empty<SerieListItemDto>();

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

        bool? statusFilter = Status switch
        {
            "ativas" => true,
            "inativas" => false,
            _ => null
        };

        var filter = new SerieListFilter(Busca, statusFilter);
        var seriesFiltradas = await _serieService.ListarAsync(filter, cancellationToken);

        if (IsProfessorOnly())
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken);
            if (escopo is not null)
            {
                seriesFiltradas = seriesFiltradas
                    .Where(x => escopo.SerieIds.Contains(x.Id))
                    .ToList();
            }
        }

        TotalCount = seriesFiltradas.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Series = seriesFiltradas
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id, string nome, int ordem, bool isAtiva, string? busca, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (!CanManageSeries())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar séries.";
            return RedirectToPage("/Series/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _serieService.AtualizarAsync(id, new SerieCreateRequest(nome, ordem, isAtiva), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Série atualizada com sucesso."
            : result.ErrorMessage ?? "Não foi possível atualizar a série.";

        return RedirectToPage("/Series/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id, string? busca, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (!CanManageSeries())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar status de séries.";
            return RedirectToPage("/Series/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var updated = await _serieService.AlternarStatusAsync(id, cancellationToken);
        TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
            ? "Status da série atualizado com sucesso."
            : "Série não encontrada para atualização de status.";

        return RedirectToPage("/Series/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, string? busca, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (!CanManageSeries())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir séries.";
            return RedirectToPage("/Series/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var deleted = await _serieService.ExcluirAsync(id, cancellationToken);
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
            ? "Série excluída com sucesso."
            : "Série não encontrada para exclusão.";

        return RedirectToPage("/Series/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    private bool CanManageSeries() => User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    private bool IsProfessorOnly() => User.IsInRole("Professor") && !CanManageSeries();
}
