using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Assistente;

namespace SistemaEscolar.Web.Pages.Assistente;

// Respostas da janela do assistente (Elson), chamadas pelo assistente.js. Disponível para qualquer usuário logado:
// o assistente só explica o uso do sistema (o mesmo conteúdo do manual, que todos já podem baixar).
public sealed class IndexModel : PageModel
{
    private readonly IAssistenteService _assistente;

    public IndexModel(IAssistenteService assistente)
    {
        _assistente = assistente;
    }

    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnGetBoasVindas() =>
        new JsonResult(_assistente.BoasVindas(PrimeiroNome(), DescreverPerfil()));

    public IActionResult OnGetPerguntar(string? q, string? topico)
    {
        var pergunta = q is { Length: > 300 } ? q[..300] : q;
        return new JsonResult(_assistente.Responder(pergunta, topico, DescreverPerfil()));
    }

    private string PrimeiroNome()
    {
        var nome = User.FindFirstValue(AppClaimTypes.FullName) ?? string.Empty;
        var primeiro = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return primeiro.Length == 0 ? string.Empty : char.ToUpper(primeiro[0]) + primeiro[1..].ToLowerInvariant();
    }

    private string DescreverPerfil()
    {
        var perfis = User.FindAll(ClaimTypes.Role).Select(c => SistemaEscolar.Application.Abstractions.Perfis.Descrever(c.Value)).Distinct().ToList();
        return perfis.Count == 0 ? "sem perfil" : string.Join(" e ", perfis);
    }
}
