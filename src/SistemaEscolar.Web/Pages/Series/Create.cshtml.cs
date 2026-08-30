using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Series;

namespace SistemaEscolar.Web.Pages.Series;

public sealed class CreateModel : PageModel
{
    private readonly ISerieService _serieService;

    public CreateModel(ISerieService serieService)
    {
        _serieService = serieService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        if (!CanManageSeries())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar séries.";
            return RedirectToPage("/Series/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!CanManageSeries())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar séries.";
            return RedirectToPage("/Series/Index");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _serieService.CriarAsync(new SerieCreateRequest(Input.Nome, Input.Ordem, Input.IsAtiva), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Não foi possível cadastrar a série.");
            return Page();
        }

        TempData["SuccessMessage"] = "Série cadastrada com sucesso.";
        return RedirectToPage("/Series/Index");
    }

    private bool CanManageSeries() => User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    public sealed class InputModel
    {
        [Required(ErrorMessage = "O nome da série é obrigatório.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Informe uma ordem válida.")]
        [Display(Name = "Ordem")]
        public int Ordem { get; set; } = 1;

        [Display(Name = "Série ativa")]
        public bool IsAtiva { get; set; } = true;
    }
}
