using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Seguranca;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages.Perfis;

public sealed class IndexModel : PageModel
{
    private readonly IPerfilService _perfilService;

    public IndexModel(IPerfilService perfilService)
    {
        _perfilService = perfilService;
    }

    public IReadOnlyList<PerfilListItemDto> Perfis { get; private set; } = Array.Empty<PerfilListItemDto>();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!User.PodeGerenciarPerfis())
        {
            TempData["ErrorMessage"] = "Seu perfil não tem acesso a esta tela.";
            return RedirectToPage("/Index");
        }

        Perfis = await _perfilService.ListarAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _perfilService.ExcluirAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Perfil excluído com sucesso."
            : result.ErrorMessage ?? "Não foi possível excluir o perfil.";
        return RedirectToPage();
    }
}
