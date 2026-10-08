using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SistemaEscolar.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    // Calculado na própria propriedade: o tratador de erros reexecuta esta página com o método original da
    // requisição (inclusive POST), e aí o OnGet não roda.
    public string? RequestId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}

