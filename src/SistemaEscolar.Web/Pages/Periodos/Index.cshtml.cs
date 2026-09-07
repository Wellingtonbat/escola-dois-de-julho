using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Periodos;

namespace SistemaEscolar.Web.Pages.Periodos;

public sealed class IndexModel : PageModel
{
    private readonly IPeriodoService _periodoService;

    public IndexModel(IPeriodoService periodoService)
    {
        _periodoService = periodoService;
    }

    public IReadOnlyList<PeriodoListItemDto> Periodos { get; private set; } = Array.Empty<PeriodoListItemDto>();

    [BindProperty(SupportsGet = true)]
    public int? AnoLetivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Trimestre { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (IsProfessorOnly())
        {
            TempData["ErrorMessage"] = "Seu perfil não tem acesso a esta tela.";
            return RedirectToPage("/Notas/Index");
        }

        bool? statusFilter = Status switch
        {
            "abertos" => true,
            "fechados" => false,
            _ => null
        };

        var filter = new PeriodoListFilter(AnoLetivo, Trimestre, statusFilter);
        Periodos = await _periodoService.ListarAsync(filter, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        int anoLetivo,
        int trimestre,
        string descricao,
        DateTime dataInicial,
        DateTime dataFinal,
        CancellationToken cancellationToken)
    {
        if (!CanManagePeriodos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar períodos.";
            return RedirectToPage();
        }

        var result = await _periodoService.CriarAsync(
            new PeriodoCreateRequest(anoLetivo, trimestre, descricao, dataInicial, dataFinal),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período cadastrado com sucesso."
            : result.ErrorMessage ?? "Não foi possível cadastrar o período.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        int anoLetivo,
        int trimestre,
        string descricao,
        DateTime dataInicial,
        DateTime dataFinal,
        CancellationToken cancellationToken)
    {
        if (!CanManagePeriodos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar períodos.";
            return RedirectToPage();
        }

        var result = await _periodoService.AtualizarAsync(
            id,
            new PeriodoCreateRequest(anoLetivo, trimestre, descricao, dataInicial, dataFinal),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período atualizado com sucesso."
            : result.ErrorMessage ?? "Não foi possível atualizar o período.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAbrirAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _periodoService.AbrirAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período aberto com sucesso."
            : result.ErrorMessage ?? "Não foi possível abrir o período.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFecharAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _periodoService.FecharAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período fechado com sucesso."
            : result.ErrorMessage ?? "Não foi possível fechar o período.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!CanManagePeriodos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir períodos.";
            return RedirectToPage();
        }

        var deleted = await _periodoService.ExcluirAsync(id, cancellationToken);
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
            ? "Período excluído com sucesso."
            : "Período não encontrado para exclusão.";

        return RedirectToPage();
    }

    private bool CanManagePeriodos() => User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    private bool IsProfessorOnly() => User.IsInRole("Professor") && !CanManagePeriodos();
}
