using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Seguranca;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages.Perfis;

// Cria um perfil (do zero ou a partir das permissões de outro) ou edita as permissões de um existente.
public sealed class EditarModel : PageModel
{
    private readonly IPerfilService _perfilService;

    public EditarModel(IPerfilService perfilService)
    {
        _perfilService = perfilService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CopiarDe { get; set; }

    [BindProperty]
    public string? Nome { get; set; }

    [BindProperty]
    public List<string> Permissoes { get; set; } = new();

    public PerfilDetalheDto? Perfil { get; private set; }

    public IReadOnlyList<PerfilListItemDto> OutrosPerfis { get; private set; } = Array.Empty<PerfilListItemDto>();

    public bool EhNovo => Id is null;

    public bool SomenteLeitura { get; private set; }

    public bool NomeEditavel => EhNovo || Perfil?.Perfil.EhPadrao == false;

    // Quem usa o próprio perfil não pode tirar dele o acesso a esta tela (ver RegrasPerfis).
    public bool PertenceAoPerfil => Perfil is not null && User.IsInRole(Perfil.Perfil.Nome);

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!User.PodeGerenciarPerfis())
        {
            TempData["ErrorMessage"] = "Seu perfil não tem acesso a esta tela.";
            return RedirectToPage("/Index");
        }

        if (!await CarregarAsync(cancellationToken))
        {
            TempData["ErrorMessage"] = "Perfil não encontrado.";
            return RedirectToPage("/Perfis/Index");
        }

        if (Perfil is not null)
        {
            Nome = Perfil.Perfil.NomeExibicao;
            Permissoes = Perfil.Perfil.Permissoes.ToList();
        }
        else if (CopiarDe is not null)
        {
            var origem = await _perfilService.ObterAsync(CopiarDe.Value, cancellationToken);
            if (origem is not null)
            {
                // Só vêm junto as permissões que quem está criando também tem (as demais não poderiam ser concedidas).
                Nome = $"{origem.Perfil.NomeExibicao} (cópia)";
                Permissoes = origem.Perfil.Permissoes.Where(User.TemPermissao).ToList();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!User.PodeGerenciarPerfis())
        {
            TempData["ErrorMessage"] = "Seu perfil não tem acesso a esta tela.";
            return RedirectToPage("/Index");
        }

        var request = new PerfilSalvarRequest(Nome, Permissoes);
        var result = Id is null
            ? await _perfilService.CriarAsync(request, cancellationToken)
            : await _perfilService.AtualizarAsync(Id.Value, request, cancellationToken);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = Id is null
                ? "Perfil criado. Para usá-lo, escolha este perfil para as contas na tela Usuários."
                : "Permissões salvas. Os usuários deste perfil passam a vê-las em até 1 minuto.";
            return RedirectToPage("/Perfis/Index");
        }

        ErrorMessage = result.ErrorMessage ?? "Não foi possível salvar o perfil.";
        if (!await CarregarAsync(cancellationToken))
        {
            TempData["ErrorMessage"] = "Perfil não encontrado.";
            return RedirectToPage("/Perfis/Index");
        }

        return Page();
    }

    private async Task<bool> CarregarAsync(CancellationToken cancellationToken)
    {
        var todos = await _perfilService.ListarAsync(cancellationToken);
        OutrosPerfis = todos.Where(x => x.Id != Id).ToList();

        if (Id is null)
        {
            return true;
        }

        Perfil = await _perfilService.ObterAsync(Id.Value, cancellationToken);
        SomenteLeitura = Perfil is null || !Perfil.Perfil.PodeEditar;
        return Perfil is not null;
    }

    // Motivo de uma permissão aparecer bloqueada na tela (ou null se pode ser marcada/desmarcada).
    public string? MotivoBloqueio(string permissao, bool marcada)
    {
        if (SomenteLeitura)
        {
            return null;
        }

        if (PertenceAoPerfil && marcada && permissao == SistemaEscolar.Application.Abstractions.Permissoes.PerfisGerenciar)
        {
            return "Você usa este perfil: não pode retirar dele o acesso a esta tela.";
        }

        return !marcada && !User.TemPermissao(permissao)
            ? "Você não tem esta permissão, então não pode concedê-la."
            : null;
    }
}
