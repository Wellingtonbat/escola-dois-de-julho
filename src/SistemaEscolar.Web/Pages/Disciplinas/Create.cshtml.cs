using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Web.Pages.Disciplinas;

public sealed class CreateModel : PageModel
{
    private readonly IDisciplinaService _disciplinaService;
    private readonly ISerieService _serieService;

    public CreateModel(IDisciplinaService disciplinaService, ISerieService serieService)
    {
        _disciplinaService = disciplinaService;
        _serieService = serieService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<SelectListItem> Series { get; private set; } = Array.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!CanManageDisciplinas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar disciplinas.";
            return RedirectToPage("/Disciplinas/Index");
        }

        Input.CargaHoraria = 40;
        await LoadSeriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!CanManageDisciplinas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar disciplinas.";
            return RedirectToPage("/Disciplinas/Index");
        }

        if (!ModelState.IsValid)
        {
            await LoadSeriesAsync();
            return Page();
        }

        var serieIds = (Input.SerieIds ?? new List<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (serieIds.Count == 0)
        {
            ModelState.AddModelError(nameof(Input.SerieIds), "Selecione ao menos uma série para a disciplina.");
            await LoadSeriesAsync();
            return Page();
        }

        var result = await _disciplinaService.CriarAsync(
            new DisciplinaCreateRequest(Input.Nome, Input.Codigo, serieIds, Input.CargaHoraria, Input.IsAtiva),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Não foi possível cadastrar a disciplina.");
            await LoadSeriesAsync();
            return Page();
        }

        TempData["SuccessMessage"] = "Disciplina cadastrada com sucesso.";
        return RedirectToPage("/Disciplinas/Index");
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

    private bool CanManageDisciplinas() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    public sealed class InputModel
    {
        [Required(ErrorMessage = "O nome da disciplina é obrigatório.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O código da disciplina é obrigatório.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Range(1, 1000, ErrorMessage = "Informe uma carga horária válida.")]
        [Display(Name = "Carga Horária")]
        public int CargaHoraria { get; set; }

        [Display(Name = "Séries vinculadas")]
        public List<Guid> SerieIds { get; set; } = new();

        [Display(Name = "Disciplina Ativa")]
        public bool IsAtiva { get; set; } = true;
    }
}
