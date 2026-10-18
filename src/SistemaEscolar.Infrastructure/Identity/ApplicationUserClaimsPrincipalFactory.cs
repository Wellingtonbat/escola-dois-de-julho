using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SistemaEscolar.Application.Abstractions;

namespace SistemaEscolar.Infrastructure.Identity;

// Acrescenta o nome completo aos claims do login (usado pela auditoria). A classe base já inclui os claims
// dos perfis (as permissões de cada um, gravadas em AspNetRoleClaims); só a marca interna de inicialização sai.
public sealed class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        foreach (var marca in identity.FindAll(Permissoes.ClaimTypeInicializado).ToList())
        {
            identity.RemoveClaim(marca);
        }

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim(AppClaimTypes.FullName, user.FullName));
        }

        return identity;
    }
}
