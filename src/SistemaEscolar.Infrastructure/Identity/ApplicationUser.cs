using Microsoft.AspNetCore.Identity;

namespace SistemaEscolar.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
}
