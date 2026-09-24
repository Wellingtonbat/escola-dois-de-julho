using Microsoft.AspNetCore.Identity;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Usuarios;

namespace SistemaEscolar.Infrastructure.Identity;

public sealed class UsuarioService : IUsuarioService
{
    // Perfis administrativos gerenciados nesta tela. O perfil Professor não entra aqui: contas de
    // professor são gerenciadas em Professores (inclusive o marcador de Vice-Diretor).
    private static readonly string[] PerfisAdministrativos =
    {
        Perfis.Diretor, Perfis.ViceDiretor, Perfis.Coordenador, Perfis.Secretaria
    };

    private const string MensagemSomenteDiretoria = "Somente Diretor ou Vice-Diretor podem gerenciar contas de Diretor e Vice-Diretor.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public UsuarioService(UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    // Só oferece os perfis que o usuário atual pode conceder.
    public IReadOnlyList<string> PerfisDisponiveis =>
        PerfisAdministrativos.Where(PodeGerenciarPerfil).ToList();

    public async Task<IReadOnlyList<UsuarioListItemDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = new List<UsuarioListItemDto>();
        var idsJaListados = new HashSet<Guid>();

        foreach (var perfil in PerfisAdministrativos)
        {
            var usuariosDoPerfil = await _userManager.GetUsersInRoleAsync(perfil);
            foreach (var usuario in usuariosDoPerfil)
            {
                // Professor que também é Vice-Diretor aparece só em Professores, nunca aqui: editar ou
                // excluir a conta por esta tela mexeria no acesso dele como professor.
                if (!idsJaListados.Add(usuario.Id) || await EhContaDeProfessorAsync(usuario))
                {
                    continue;
                }

                usuarios.Add(MontarDto(usuario, perfil));
            }
        }

        return usuarios
            .OrderBy(x => x.NomeCompleto)
            .ToList();
    }

    public async Task<UsuarioListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null)
        {
            return null;
        }

        var perfil = await ObterPerfilAsync(usuario);
        return perfil is null ? null : MontarDto(usuario, perfil);
    }

    public async Task<UsuarioCreateResult> CriarAsync(UsuarioCreateRequest request, CancellationToken cancellationToken = default)
    {
        var nome = (request.NomeCompleto ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var cpf = NormalizeCpf(request.Cpf);
        var perfil = (request.Perfil ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return UsuarioCreateResult.Fail("O nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return UsuarioCreateResult.Fail("O e-mail é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(cpf))
        {
            return UsuarioCreateResult.Fail("O CPF é obrigatório.");
        }

        if (!PerfisAdministrativos.Contains(perfil))
        {
            return UsuarioCreateResult.Fail("Selecione um perfil válido.");
        }

        if (!PodeGerenciarPerfil(perfil))
        {
            return UsuarioCreateResult.Fail(MensagemSomenteDiretoria);
        }

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            return UsuarioCreateResult.Fail("Defina a senha padrão do usuário.");
        }

        if (await _userManager.FindByNameAsync(cpf) is not null)
        {
            return UsuarioCreateResult.Fail("Já existe um usuário cadastrado com este CPF.");
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return UsuarioCreateResult.Fail("Já existe um usuário cadastrado com este e-mail.");
        }

        var novoUsuario = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = cpf,
            Email = email,
            FullName = nome,
            IsActive = request.IsAtivo,
            EmailConfirmed = true,
            MustChangePassword = true
        };

        var createResult = await _userManager.CreateAsync(novoUsuario, request.Senha);
        if (!createResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(createResult));
        }

        var roleResult = await _userManager.AddToRoleAsync(novoUsuario, perfil);
        if (!roleResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(roleResult));
        }

        return UsuarioCreateResult.Success();
    }

    public async Task<UsuarioCreateResult> AtualizarAsync(Guid id, UsuarioCreateRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null)
        {
            return UsuarioCreateResult.Fail("Usuário não encontrado.");
        }

        var perfilAtual = await ObterPerfilAsync(usuario);
        if (perfilAtual is null)
        {
            return UsuarioCreateResult.Fail("Usuário não encontrado.");
        }

        var nome = (request.NomeCompleto ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var cpf = NormalizeCpf(request.Cpf);
        var perfil = (request.Perfil ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return UsuarioCreateResult.Fail("O nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return UsuarioCreateResult.Fail("O e-mail é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(cpf))
        {
            return UsuarioCreateResult.Fail("O CPF é obrigatório.");
        }

        if (!PerfisAdministrativos.Contains(perfil))
        {
            return UsuarioCreateResult.Fail("Selecione um perfil válido.");
        }

        // Vale para o perfil atual (não editar a conta de um Diretor) e para o novo (não promover ninguém).
        if (!PodeGerenciarPerfil(perfilAtual) || !PodeGerenciarPerfil(perfil))
        {
            return UsuarioCreateResult.Fail(MensagemSomenteDiretoria);
        }

        var usuarioComMesmoCpf = await _userManager.FindByNameAsync(cpf);
        if (usuarioComMesmoCpf is not null && usuarioComMesmoCpf.Id != usuario.Id)
        {
            return UsuarioCreateResult.Fail("Já existe um usuário cadastrado com este CPF.");
        }

        var usuarioComMesmoEmail = await _userManager.FindByEmailAsync(email);
        if (usuarioComMesmoEmail is not null && usuarioComMesmoEmail.Id != usuario.Id)
        {
            return UsuarioCreateResult.Fail("Já existe um usuário cadastrado com este e-mail.");
        }

        var setUserNameResult = await _userManager.SetUserNameAsync(usuario, cpf);
        if (!setUserNameResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(setUserNameResult));
        }

        var setEmailResult = await _userManager.SetEmailAsync(usuario, email);
        if (!setEmailResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(setEmailResult));
        }

        usuario.FullName = nome;
        usuario.IsActive = request.IsAtivo;

        var updateResult = await _userManager.UpdateAsync(usuario);
        if (!updateResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(updateResult));
        }

        if (!string.Equals(perfil, perfilAtual, StringComparison.Ordinal))
        {
            await _userManager.RemoveFromRoleAsync(usuario, perfilAtual);
            await _userManager.AddToRoleAsync(usuario, perfil);
            await _userManager.UpdateSecurityStampAsync(usuario);
        }

        return UsuarioCreateResult.Success();
    }

    public async Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null || !await PodeGerenciarUsuarioAsync(usuario))
        {
            return false;
        }

        usuario.IsActive = !usuario.IsActive;
        var result = await _userManager.UpdateAsync(usuario);
        return result.Succeeded;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null || !await PodeGerenciarUsuarioAsync(usuario))
        {
            return false;
        }

        var result = await _userManager.DeleteAsync(usuario);
        return result.Succeeded;
    }

    public async Task<UsuarioCreateResult> RedefinirSenhaAsync(Guid id, string novaSenha, CancellationToken cancellationToken = default)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null || await ObterPerfilAsync(usuario) is null)
        {
            return UsuarioCreateResult.Fail("Usuário não encontrado.");
        }

        if (!await PodeGerenciarUsuarioAsync(usuario))
        {
            return UsuarioCreateResult.Fail(MensagemSomenteDiretoria);
        }

        if (string.IsNullOrWhiteSpace(novaSenha))
        {
            return UsuarioCreateResult.Fail("Informe a nova senha padrão.");
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var resetResult = await _userManager.ResetPasswordAsync(usuario, resetToken, novaSenha);
        if (!resetResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(resetResult));
        }

        usuario.MustChangePassword = true;
        await _userManager.UpdateAsync(usuario);

        return UsuarioCreateResult.Success();
    }

    // Perfil administrativo da conta, ou null se ela não é gerenciada nesta tela (não tem perfil
    // administrativo ou é uma conta de professor).
    private async Task<string?> ObterPerfilAsync(ApplicationUser usuario)
    {
        var roles = await _userManager.GetRolesAsync(usuario);
        if (roles.Contains(Perfis.Professor))
        {
            return null;
        }

        return PerfisAdministrativos.FirstOrDefault(roles.Contains);
    }

    private async Task<bool> EhContaDeProfessorAsync(ApplicationUser usuario) =>
        await _userManager.IsInRoleAsync(usuario, Perfis.Professor);

    private bool PodeGerenciarPerfil(string perfil) =>
        PermissoesPerfil.PodeGerenciarContaComPerfil(_currentUserService.IsInRole, perfil);

    // Conta existente, gerenciada nesta tela e cujo perfil o usuário atual pode administrar.
    private async Task<bool> PodeGerenciarUsuarioAsync(ApplicationUser usuario)
    {
        var perfil = await ObterPerfilAsync(usuario);
        return perfil is not null && PodeGerenciarPerfil(perfil);
    }

    private static UsuarioListItemDto MontarDto(ApplicationUser usuario, string perfil) =>
        new(usuario.Id, usuario.FullName ?? string.Empty, usuario.Email ?? string.Empty, usuario.UserName ?? string.Empty, perfil, usuario.IsActive);

    private static string DescreverErros(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));

    private static string NormalizeCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        return new string(cpf.Where(char.IsDigit).ToArray());
    }
}
