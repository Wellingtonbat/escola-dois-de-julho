using Microsoft.AspNetCore.Identity;
using SistemaEscolar.Application.Abstractions;

namespace SistemaEscolar.Infrastructure.Identity;

public sealed class AccountProvisioner : IAccountProvisioner
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountProvisioner(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AccountProvisionResult> CriarOuReiniciarContaAsync(
        string cpf,
        string nomeCompleto,
        string email,
        string senha,
        string role,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByNameAsync(cpf);

        if (existingUser is null)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = cpf,
                Email = email,
                FullName = nomeCompleto,
                IsActive = true,
                EmailConfirmed = true,
                MustChangePassword = true
            };

            var createResult = await _userManager.CreateAsync(user, senha);
            if (!createResult.Succeeded)
            {
                return AccountProvisionResult.Fail(DescreverErros(createResult));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                return AccountProvisionResult.Fail(DescreverErros(roleResult));
            }

            return AccountProvisionResult.Success();
        }

        // CPF já possuía conta (ex.: usuário reativado após exclusão) - reaproveita a conta,
        // redefine a senha padrão informada e exige troca no próximo login.
        existingUser.FullName = nomeCompleto;
        existingUser.Email = email;
        existingUser.IsActive = true;
        existingUser.MustChangePassword = true;

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
        var resetResult = await _userManager.ResetPasswordAsync(existingUser, resetToken, senha);
        if (!resetResult.Succeeded)
        {
            return AccountProvisionResult.Fail(DescreverErros(resetResult));
        }

        await _userManager.UpdateAsync(existingUser);

        if (!await _userManager.IsInRoleAsync(existingUser, role))
        {
            await _userManager.AddToRoleAsync(existingUser, role);
        }

        return AccountProvisionResult.Success();
    }

    private static string DescreverErros(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}
