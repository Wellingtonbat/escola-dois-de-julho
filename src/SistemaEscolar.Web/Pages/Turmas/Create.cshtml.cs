using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Web.Pages.Turmas;

public sealed class CreateModel : PageModel
{
    private readonly ITurmaService _turmaService;
    private readonly ISerieService _serieService;

    public CreateModel(ITurmaService turmaService, ISerieService serieService)
    {
        _turmaService = turmaService;
        _serieService = serieService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<SelectListItem> Series { get; private set; } = Array.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!CanManageTurmas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar turmas.";
            return RedirectToPage("/Turmas/Index");
        }

        Input.AnoLetivo = DateTime.UtcNow.Year;
        await LoadSeriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!CanManageTurmas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar turmas.";
            return RedirectToPage("/Turmas/Index");
        }

        if (!ModelState.IsValid)
        {
            await LoadSeriesAsync();
            return Page();
        }

        var result = await _turmaService.CriarAsync(
            new TurmaCreateRequest(Input.Nome, Input.SerieId, Input.Turno, Input.AnoLetivo, Input.IsAtiva),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Não foi possível cadastrar a turma.");
            await LoadSeriesAsync();
            return Page();
        }

        TempData["SuccessMessage"] = "Turma cadastrada com sucesso.";
        return RedirectToPage("/Turmas/Index");
    }

    private async Task LoadSeriesAsync()
    {
        var series = await _serieService.ListarAsync(new SerieListFilter(null, true));
        Series = series
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.Id.ToString()))
            .ToList();
    }

    private bool CanManageTurmas() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    public sealed class InputModel
    {
        [Required(ErrorMessage = "O nome da turma é obrigatório.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A série é obrigatória.")]
        [Display(Name = "Série")]
        public Guid SerieId { get; set; }

        [Display(Name = "Turno")]
        public string Turno { get; set; } = string.Empty;

        [Range(2000, 2100, ErrorMessage = "Informe um ano letivo válido.")]
        [Display(Name = "Ano Letivo")]
        public int AnoLetivo { get; set; }

        [Display(Name = "Turma Ativa")]
        public bool IsAtiva { get; set; } = true;
    }
}
