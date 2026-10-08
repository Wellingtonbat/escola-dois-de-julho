using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Atas;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages.Atas;

public sealed class IndexModel : PageModel
{
    private readonly IAtaService _ataService;

    public IndexModel(IAtaService ataService)
    {
        _ataService = ataService;
    }

    public IReadOnlyList<AtaListItemDto> Atas { get; private set; } = Array.Empty<AtaListItemDto>();
    public IReadOnlyList<SelectListItem> TurmasSemAta { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = Array.Empty<int>();

    [BindProperty(SupportsGet = true)]
    public int? AnoLetivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!User.PodeAcessarAtas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para acessar as Atas.";
            return RedirectToPage("/Index");
        }

        var todas = await _ataService.ListarAsync(null, cancellationToken);
        AnosDisponiveis = todas.Select(x => x.AnoLetivo).Distinct().OrderByDescending(x => x).ToList();

        var anoSelecionado = AnoLetivo ?? AnosDisponiveis.FirstOrDefault(HorarioBrasilia.Agora.Year);

        var filtradas = todas.Where(x => x.AnoLetivo == anoSelecionado);
        filtradas = Status switch
        {
            "rascunho" => filtradas.Where(x => x.Status == AtaConstantes.StatusRascunho),
            "finalizada" => filtradas.Where(x => x.Status == AtaConstantes.StatusFinalizada),
            _ => filtradas
        };

        Atas = filtradas.ToList();
        AnoLetivo = anoSelecionado;

        TurmasSemAta = (await _ataService.ListarTurmasSemAtaAsync(anoSelecionado, cancellationToken))
            .Select(x => new SelectListItem($"{x.TurmaNome} ({x.SerieNome})", x.TurmaId.ToString()))
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostCriarAsync(Guid turmaId, int? anoLetivo, string? status, CancellationToken cancellationToken)
    {
        if (!User.PodeAcessarAtas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para criar Atas.";
            return RedirectToPage("/Atas/Index", new { AnoLetivo = anoLetivo, Status = status });
        }

        var (result, ataId) = await _ataService.CriarParaTurmaAsync(turmaId, cancellationToken);
        if (!result.Succeeded || ataId is null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível criar a Ata.";
            return RedirectToPage("/Atas/Index", new { AnoLetivo = anoLetivo, Status = status });
        }

        return RedirectToPage("/Atas/Detalhe", new { id = ataId });
    }
}
