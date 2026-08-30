using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Infrastructure.Identity;

namespace SistemaEscolar.Web.Pages;

public sealed class TrocarSenhaModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public TrocarSenhaModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool ForcaTrocaSenha { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Login");
        }

        ForcaTrocaSenha = user.MustChangePassword;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Login");
        }

        ForcaTrocaSenha = user.MustChangePassword;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, Input.SenhaAtual, Input.NovaSenha);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "Senha alterada com sucesso.";
        return RedirectToPage("/Index");
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Informe a senha atual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha Atual")]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a nova senha.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "A nova senha deve possuir ao menos 8 caracteres.")]
        [Display(Name = "Nova Senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a nova senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NovaSenha), ErrorMessage = "A confirmação não corresponde à nova senha.")]
        [Display(Name = "Confirmar Nova Senha")]
        public string ConfirmarNovaSenha { get; set; } = string.Empty;
    }
}
