using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Infrastructure.Identity;

namespace SistemaEscolar.Web.Pages;

public sealed class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; set; } = "/";

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var cpf = NormalizeCpf(Input.Cpf);
        var user = await _userManager.FindByNameAsync(cpf);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "CPF ou senha inválidos.");
            return Page();
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Usuário inativo. Procure a direção.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Usuário bloqueado temporariamente por tentativas inválidas.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "CPF ou senha inválidos.");
        return Page();
    }

    private static string NormalizeCpf(string cpf)
    {
        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Informe o CPF.")]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Manter conectado")]
        public bool RememberMe { get; set; }
    }
}
