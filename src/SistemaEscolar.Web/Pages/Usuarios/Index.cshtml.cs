using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Usuarios;

namespace SistemaEscolar.Web.Pages.Usuarios;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly IUsuarioService _usuarioService;

    public IndexModel(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    public IReadOnlyList<UsuarioListItemDto> Usuarios { get; private set; } = Array.Empty<UsuarioListItemDto>();
    public IReadOnlyList<SelectListItem> Perfis { get; private set; } = Array.Empty<SelectListItem>();

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
        if (!CanManageUsuarios())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para acessar a gestão de usuários.";
            return RedirectToPage("/Index");
        }

        Perfis = _usuarioService.PerfisDisponiveis
            .Select(x => new SelectListItem(TraduzirPerfil(x), x))
            .ToList();

        var usuarios = await _usuarioService.ListarAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(Busca))
        {
            var busca = Busca.Trim();
            usuarios = usuarios
                .Where(x =>
                    x.NomeCompleto.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    x.Email.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    x.Cpf.Contains(busca, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        usuarios = Status switch
        {
            "ativos" => usuarios.Where(x => x.IsAtivo).ToList(),
            "inativos" => usuarios.Where(x => !x.IsAtivo).ToList(),
            _ => usuarios
        };

        TotalCount = usuarios.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Usuarios = usuarios
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string nomeCompleto,
        string email,
        string cpf,
        string perfil,
        string senha,
        bool isAtivo,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageUsuarios())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar usuários.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _usuarioService.CriarAsync(
            new UsuarioCreateRequest(nomeCompleto, email, cpf, perfil, isAtivo, senha),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível cadastrar o usuário.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Usuário cadastrado com sucesso. Ele deverá trocar a senha padrão no primeiro acesso.";
        return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        string nomeCompleto,
        string email,
        string cpf,
        string perfil,
        bool isAtivo,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageUsuarios())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar usuários.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _usuarioService.AtualizarAsync(
            id,
            new UsuarioCreateRequest(nomeCompleto, email, cpf, perfil, isAtivo),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível atualizar o usuário.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Usuário atualizado com sucesso.";
        return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(
        Guid id,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageUsuarios())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar status de usuários.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (EhUsuarioAtual(id))
        {
            TempData["ErrorMessage"] = "Você não pode alterar o status do seu próprio usuário.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var updated = await _usuarioService.AlternarStatusAsync(id, cancellationToken);
        if (!updated)
        {
            TempData["ErrorMessage"] = "Usuário não encontrado para atualização de status.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Status do usuário atualizado com sucesso.";
        return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid id,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageUsuarios())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir usuários.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (EhUsuarioAtual(id))
        {
            TempData["ErrorMessage"] = "Você não pode excluir o seu próprio usuário.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var deleted = await _usuarioService.ExcluirAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Usuário não encontrado para exclusão.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Usuário excluído com sucesso.";
        return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostRedefinirSenhaAsync(
        Guid id,
        string novaSenha,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageUsuarios())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para redefinir a senha de usuários.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _usuarioService.RedefinirSenhaAsync(id, novaSenha, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível redefinir a senha do usuário.";
            return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Senha redefinida com sucesso. O usuário deverá trocá-la no próximo acesso.";
        return RedirectToPage("/Usuarios/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    private bool CanManageUsuarios() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    private bool EhUsuarioAtual(Guid id)
    {
        var usuarioAtualId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(usuarioAtualId, out var atualId) && atualId == id;
    }

    private static string TraduzirPerfil(string perfil) => perfil switch
    {
        "Secretaria" => "Secretária",
        _ => perfil
    };
}
