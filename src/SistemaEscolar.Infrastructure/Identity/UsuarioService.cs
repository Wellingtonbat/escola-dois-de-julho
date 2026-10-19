using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Seguranca;
using SistemaEscolar.Application.Usuarios;

namespace SistemaEscolar.Infrastructure.Identity;

public sealed class UsuarioService : IUsuarioService
{
    // Perfis que não são atribuídos nesta tela: contas de professor são gerenciadas em Professores (inclusive o
    // marcador de Vice-Diretor), e o perfil com a grafia antiga de Coordenador não é mais oferecido.
    private static readonly HashSet<string> PerfisForaDestaTela = new(StringComparer.OrdinalIgnoreCase)
    {
        Perfis.Professor, Perfis.CoordenadorLegado
    };

    private const string MensagemSomenteDiretoria =
        "Você só pode gerenciar contas cujo perfil não tenha mais permissões do que o seu.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPerfilService _perfilService;
    private readonly IProfessorRepository _professorRepository;

    public UsuarioService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ICurrentUserService currentUserService,
        IPerfilService perfilService,
        IProfessorRepository professorRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUserService = currentUserService;
        _perfilService = perfilService;
        _professorRepository = professorRepository;
    }

    // Só oferece os perfis que o usuário atual pode conceder.
    public async Task<IReadOnlyList<string>> ListarPerfisDisponiveisAsync(CancellationToken cancellationToken = default)
    {
        var disponiveis = new List<string>();
        foreach (var perfil in await ListarPerfisAdministrativosAsync(cancellationToken))
        {
            if (await PodeGerenciarPerfilAsync(perfil, cancellationToken))
            {
                disponiveis.Add(perfil);
            }
        }

        return disponiveis;
    }

    public async Task<IReadOnlyList<UsuarioListItemDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = new List<UsuarioListItemDto>();
        var idsJaListados = new HashSet<Guid>();

        foreach (var perfil in await ListarPerfisAdministrativosAsync(cancellationToken))
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

        var perfil = await ObterPerfilAsync(usuario, cancellationToken);
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

        if (!(await ListarPerfisAdministrativosAsync(cancellationToken)).Contains(perfil))
        {
            return UsuarioCreateResult.Fail("Selecione um perfil válido.");
        }

        if (!await PodeGerenciarPerfilAsync(perfil, cancellationToken))
        {
            return UsuarioCreateResult.Fail(MensagemSomenteDiretoria);
        }

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            return UsuarioCreateResult.Fail("Defina a senha padrão do usuário.");
        }

        var contaExistente = await _userManager.FindByNameAsync(cpf);
        if (contaExistente is not null)
        {
            return await ReaproveitarContaDeProfessorAsync(contaExistente, nome, email, perfil, request, cancellationToken);
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

        var perfilAtual = await ObterPerfilAsync(usuario, cancellationToken);
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

        if (!(await ListarPerfisAdministrativosAsync(cancellationToken)).Contains(perfil))
        {
            return UsuarioCreateResult.Fail("Selecione um perfil válido.");
        }

        // Vale para a conta como está (não editar quem tem mais acesso) e para o novo perfil (não promover ninguém).
        if (!await PodeGerenciarUsuarioAsync(usuario, cancellationToken) || !await PodeGerenciarPerfilAsync(perfil, cancellationToken))
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
        if (usuario is null || !await PodeGerenciarUsuarioAsync(usuario, cancellationToken))
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
        if (usuario is null || !await PodeGerenciarUsuarioAsync(usuario, cancellationToken))
        {
            return false;
        }

        var result = await _userManager.DeleteAsync(usuario);
        return result.Succeeded;
    }

    public async Task<UsuarioCreateResult> RedefinirSenhaAsync(Guid id, string novaSenha, CancellationToken cancellationToken = default)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null || await ObterPerfilAsync(usuario, cancellationToken) is null)
        {
            return UsuarioCreateResult.Fail("Usuário não encontrado.");
        }

        if (!await PodeGerenciarUsuarioAsync(usuario, cancellationToken))
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

    // Inativar ou excluir um professor não apaga o login dele (o CPF continua em uso). Quando essa pessoa passa a
    // ter outra função na escola (ex.: professora que vira coordenadora), a conta é reaproveitada: perde o perfil
    // Professor, recebe o perfil escolhido, os dados e a senha novos. O cadastro de professor continua inativo,
    // com o histórico de notas intacto.
    private async Task<UsuarioCreateResult> ReaproveitarContaDeProfessorAsync(
        ApplicationUser conta, string nome, string email, string perfil, UsuarioCreateRequest request, CancellationToken cancellationToken)
    {
        var perfisAtuais = await _userManager.GetRolesAsync(conta);
        if (!perfisAtuais.Contains(Perfis.Professor))
        {
            return UsuarioCreateResult.Fail("Já existe um usuário cadastrado com este CPF.");
        }

        var professor = await _professorRepository.GetByUsuarioCpfAsync(conta.UserName!, cancellationToken);
        if (professor is { IsAtivo: true })
        {
            return UsuarioCreateResult.Fail(
                $"Este CPF é de um professor ativo ({professor.NomeCompleto}). Para transformar a conta em outro perfil, " +
                "inative primeiro o cadastro dele na tela Professores.");
        }

        if (!await _perfilService.UsuarioAtualCobrePerfisAsync(perfisAtuais, cancellationToken))
        {
            return UsuarioCreateResult.Fail(MensagemSomenteDiretoria);
        }

        var contaComMesmoEmail = await _userManager.FindByEmailAsync(email);
        if (contaComMesmoEmail is not null && contaComMesmoEmail.Id != conta.Id)
        {
            return UsuarioCreateResult.Fail("Já existe um usuário cadastrado com este e-mail.");
        }

        // A senha vem primeiro: se ela for recusada, nada na conta é alterado.
        var token = await _userManager.GeneratePasswordResetTokenAsync(conta);
        var senhaResult = await _userManager.ResetPasswordAsync(conta, token, request.Senha!);
        if (!senhaResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(senhaResult));
        }

        var emailResult = await _userManager.SetEmailAsync(conta, email);
        if (!emailResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(emailResult));
        }

        conta.FullName = nome;
        conta.IsActive = request.IsAtivo;
        conta.EmailConfirmed = true;
        conta.MustChangePassword = true;
        await _userManager.UpdateAsync(conta);

        await _userManager.RemoveFromRolesAsync(conta, perfisAtuais);
        var perfilResult = await _userManager.AddToRoleAsync(conta, perfil);
        if (!perfilResult.Succeeded)
        {
            return UsuarioCreateResult.Fail(DescreverErros(perfilResult));
        }

        await _userManager.UpdateSecurityStampAsync(conta);
        return UsuarioCreateResult.Success(
            $"Este CPF já tinha um acesso de professor (inativo). A conta foi reaproveitada com o perfil {Perfis.Descrever(perfil)}; " +
            "a pessoa deverá trocar a senha padrão no primeiro acesso.");
    }

    // Perfil administrativo da conta, ou null se ela não é gerenciada nesta tela (não tem perfil
    // administrativo ou é uma conta de professor).
    private async Task<string?> ObterPerfilAsync(ApplicationUser usuario, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(usuario);
        if (roles.Contains(Perfis.Professor))
        {
            return null;
        }

        return (await ListarPerfisAdministrativosAsync(cancellationToken)).FirstOrDefault(roles.Contains);
    }

    // Perfis atribuíveis nesta tela: os que vêm com o sistema e os criados em "Perfis e Permissões".
    private async Task<IReadOnlyList<string>> ListarPerfisAdministrativosAsync(CancellationToken cancellationToken) =>
        (await _roleManager.Roles.AsNoTracking().Select(x => x.Name!).ToListAsync(cancellationToken))
        .Where(x => !PerfisForaDestaTela.Contains(x))
        .OrderBy(RegrasPerfis.Ordem)
        .ThenBy(x => x, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    private async Task<bool> EhContaDeProfessorAsync(ApplicationUser usuario) =>
        await _userManager.IsInRoleAsync(usuario, Perfis.Professor);

    // Conceder um perfil exige ter todas as permissões dele (regra contra escalonamento).
    private async Task<bool> PodeGerenciarPerfilAsync(string perfil, CancellationToken cancellationToken) =>
        PermissoesPerfil.PodeGerenciarUsuarios(_currentUserService.TemPermissao)
        && await _perfilService.UsuarioAtualCobrePerfisAsync(new[] { perfil }, cancellationToken);

    // Conta existente, gerenciada nesta tela, e o usuário atual tem todas as permissões de todos os perfis dela.
    private async Task<bool> PodeGerenciarUsuarioAsync(ApplicationUser usuario, CancellationToken cancellationToken)
    {
        var perfil = await ObterPerfilAsync(usuario, cancellationToken);
        return perfil is not null
            && PermissoesPerfil.PodeGerenciarUsuarios(_currentUserService.TemPermissao)
            && await _perfilService.UsuarioAtualCobrePerfisAsync(await _userManager.GetRolesAsync(usuario), cancellationToken);
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
