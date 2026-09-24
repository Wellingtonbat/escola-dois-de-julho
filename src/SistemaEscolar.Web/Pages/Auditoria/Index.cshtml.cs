using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Auditoria;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages.Auditoria;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private const int DiasPadrao = 30;

    // Horário de Brasília (UTC-3), o mesmo usado pela AuditoriaService.
    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3);

    private readonly IAuditoriaService _auditoriaService;

    public IndexModel(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    public IReadOnlyList<AuditoriaItemDto> Registros { get; private set; } = Array.Empty<AuditoriaItemDto>();
    public IReadOnlyList<SelectListItem> Entidades { get; private set; } = Array.Empty<SelectListItem>();

    [BindProperty(SupportsGet = true)]
    public DateTime? De { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Ate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Usuario { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Entidade { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Acao { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = PageSizeFixo;

    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; } = 1;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!User.PodeVerAuditoria())
        {
            TempData["ErrorMessage"] = "Somente Diretor ou Vice-Diretor podem consultar a auditoria.";
            return RedirectToPage("/Index");
        }

        // Primeira abertura da tela: últimos 30 dias. Se o usuário limpar uma data e filtrar, respeita a escolha.
        var hoje = (DateTime.UtcNow + OffsetBrasilia).Date;
        if (!Request.Query.ContainsKey(nameof(De)))
        {
            De = hoje.AddDays(-DiasPadrao);
        }

        if (!Request.Query.ContainsKey(nameof(Ate)))
        {
            Ate = hoje;
        }

        Entidades = _auditoriaService.Entidades
            .Select(x => new SelectListItem(x.Rotulo, x.Tabela))
            .ToList();

        var resultado = await _auditoriaService.ListarAsync(
            new AuditoriaFiltro(De, Ate, Usuario, Entidade, Acao, PageNumber, PageSize),
            cancellationToken);

        if (!resultado.Succeeded || resultado.Pagina is null)
        {
            TempData["ErrorMessage"] = resultado.ErrorMessage ?? "Não foi possível consultar a auditoria.";
            return RedirectToPage("/Index");
        }

        Registros = resultado.Pagina.Itens;
        TotalCount = resultado.Pagina.Total;
        TotalPages = resultado.Pagina.TotalPaginas;
        PageNumber = Math.Clamp(resultado.Pagina.Pagina, 1, TotalPages);

        return Page();
    }
}
