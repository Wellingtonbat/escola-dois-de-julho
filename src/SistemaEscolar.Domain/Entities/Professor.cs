using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

public sealed class Professor : BaseEntity
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UsuarioCpf { get; set; } = string.Empty;
    public bool IsAtivo { get; set; } = true;
}
