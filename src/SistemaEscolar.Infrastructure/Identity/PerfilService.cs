using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Seguranca;
using SistemaEscolar.Infrastructure.Persistence;

namespace SistemaEscolar.Infrastructure.Identity;

// Perfis (roles do Identity) e suas permissões (claims em AspNetRoleClaims). As regras de quem pode o quê
// ficam em RegrasPerfis; aqui só se lê e grava. Quem já está logado recebe as permissões novas em até um
// minuto, quando o cookie é revalidado (ver SecurityStampValidatorOptions em DependencyInjection).
public sealed class PerfilService : IPerfilService
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ICurrentUserService _currentUserService;

    public PerfilService(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager, ICurrentUserService currentUserService)
    {
        _context = context;
        _roleManager = roleManager;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<PerfilListItemDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var perfis = await _context.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var permissoes = await CarregarPermissoesAsync(null, cancellationToken);
        var totais = await _context.UserRoles.AsNoTracking()
            .GroupBy(x => x.RoleId)
            .Select(g => new { RoleId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Total, cancellationToken);

        return perfis
            .Select(p => MontarDto(p, permissoes.GetValueOrDefault(p.Id) ?? new List<string>(), totais.GetValueOrDefault(p.Id)))
            .OrderBy(x => RegrasPerfis.Ordem(x.Nome))
            .ThenBy(x => x.NomeExibicao, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<PerfilDetalheDto?> ObterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var perfil = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (perfil is null)
        {
            return null;
        }

        var permissoes = (await CarregarPermissoesAsync(id, cancellationToken)).GetValueOrDefault(id) ?? new List<string>();
        var usuarios = await (
                from vinculo in _context.UserRoles.AsNoTracking()
                join usuario in _context.Users.AsNoTracking() on vinculo.UserId equals usuario.Id
                where vinculo.RoleId == id
                select usuario.FullName ?? usuario.UserName ?? string.Empty)
            .ToListAsync(cancellationToken);

        return new PerfilDetalheDto(
            MontarDto(perfil, permissoes, usuarios.Count),
            usuarios.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList());
    }

    public async Task<PerfilResult> CriarAsync(PerfilSalvarRequest request, CancellationToken cancellationToken = default)
    {
        var novas = Normalizar(request.Permissoes);
        var erro = RegrasPerfis.ValidarCriacao(_currentUserService.TemPermissao, novas)
            ?? RegrasPerfis.ValidarNome(request.Nome, null, await ListarNomesAsync(cancellationToken));
        if (erro is not null)
        {
            return PerfilResult.Fail(erro);
        }

        var perfil = new ApplicationRole { Id = Guid.NewGuid(), Name = request.Nome!.Trim() };
        var criado = await _roleManager.CreateAsync(perfil);
        if (!criado.Succeeded)
        {
            return PerfilResult.Fail(string.Join(" ", criado.Errors.Select(e => e.Description)));
        }

        _context.RoleClaims.Add(new IdentityRoleClaim<Guid>
        {
            RoleId = perfil.Id, ClaimType = Permissoes.ClaimTypeInicializado,
            ClaimValue = Permissoes.VersaoAtual.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        foreach (var permissao in novas)
        {
            _context.RoleClaims.Add(NovaPermissao(perfil.Id, permissao));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return PerfilResult.Success(perfil.Id);
    }

    public async Task<PerfilResult> AtualizarAsync(Guid id, PerfilSalvarRequest request, CancellationToken cancellationToken = default)
    {
        var perfil = await _roleManager.FindByIdAsync(id.ToString());
        if (perfil?.Name is null)
        {
            return PerfilResult.Fail("Perfil não encontrado.");
        }

        var claims = await _context.RoleClaims
            .Where(x => x.RoleId == id && x.ClaimType == Permissoes.ClaimType)
            .ToListAsync(cancellationToken);
        var atuais = claims.Select(x => x.ClaimValue!).ToList();
        var novas = Normalizar(request.Permissoes);

        var erro = RegrasPerfis.ValidarAlteracao(
            _currentUserService.TemPermissao, perfil.Name, atuais, novas, _currentUserService.IsInRole(perfil.Name));
        if (erro is not null)
        {
            return PerfilResult.Fail(erro);
        }

        var nome = (request.Nome ?? string.Empty).Trim();
        if (!string.Equals(nome, perfil.Name, StringComparison.Ordinal) && !string.Equals(nome, Perfis.Descrever(perfil.Name), StringComparison.Ordinal))
        {
            if (RegrasPerfis.EhPadrao(perfil.Name))
            {
                return PerfilResult.Fail("Os perfis que vêm com o sistema não podem ser renomeados.");
            }

            var erroNome = RegrasPerfis.ValidarNome(nome, id, await ListarNomesAsync(cancellationToken));
            if (erroNome is not null)
            {
                return PerfilResult.Fail(erroNome);
            }

            perfil.Name = nome;
            var renomeado = await _roleManager.UpdateAsync(perfil);
            if (!renomeado.Succeeded)
            {
                return PerfilResult.Fail(string.Join(" ", renomeado.Errors.Select(e => e.Description)));
            }
        }

        _context.RoleClaims.RemoveRange(claims.Where(x => !novas.Contains(x.ClaimValue!)));
        foreach (var permissao in novas.Except(atuais))
        {
            _context.RoleClaims.Add(NovaPermissao(id, permissao));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return PerfilResult.Success(id);
    }

    public async Task<PerfilResult> ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var perfil = await _roleManager.FindByIdAsync(id.ToString());
        if (perfil?.Name is null)
        {
            return PerfilResult.Fail("Perfil não encontrado.");
        }

        var atuais = (await CarregarPermissoesAsync(id, cancellationToken)).GetValueOrDefault(id) ?? new List<string>();
        var totalUsuarios = await _context.UserRoles.CountAsync(x => x.RoleId == id, cancellationToken);
        var erro = RegrasPerfis.ValidarExclusao(_currentUserService.TemPermissao, perfil.Name, atuais, totalUsuarios);
        if (erro is not null)
        {
            return PerfilResult.Fail(erro);
        }

        var resultado = await _roleManager.DeleteAsync(perfil);
        return resultado.Succeeded
            ? PerfilResult.Success(id)
            : PerfilResult.Fail(string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }

    public async Task<bool> UsuarioAtualCobrePerfisAsync(IEnumerable<string> perfis, CancellationToken cancellationToken = default)
    {
        var normalizados = perfis.Select(x => x.ToUpperInvariant()).ToList();
        var permissoes = await (
                from claim in _context.RoleClaims.AsNoTracking()
                join perfil in _context.Roles.AsNoTracking() on claim.RoleId equals perfil.Id
                where claim.ClaimType == Permissoes.ClaimType && normalizados.Contains(perfil.NormalizedName!)
                select claim.ClaimValue!)
            .Distinct()
            .ToListAsync(cancellationToken);

        return PermissoesPerfil.CobrePermissoes(_currentUserService.TemPermissao, permissoes);
    }

    private PerfilListItemDto MontarDto(ApplicationRole perfil, IReadOnlyList<string> permissoes, int totalUsuarios)
    {
        var nome = perfil.Name ?? string.Empty;
        return new PerfilListItemDto(
            perfil.Id,
            nome,
            Perfis.Descrever(nome),
            RegrasPerfis.EhPadrao(nome),
            RegrasPerfis.EhSomenteLeitura(nome),
            totalUsuarios,
            permissoes,
            RegrasPerfis.PodeEditar(_currentUserService.TemPermissao, nome, permissoes),
            RegrasPerfis.PodeExcluir(_currentUserService.TemPermissao, nome, permissoes, totalUsuarios));
    }

    // Permissões por perfil, só as que ainda existem no catálogo e na ordem em que aparecem nele.
    private async Task<Dictionary<Guid, List<string>>> CarregarPermissoesAsync(Guid? perfilId, CancellationToken cancellationToken)
    {
        var consulta = _context.RoleClaims.AsNoTracking().Where(x => x.ClaimType == Permissoes.ClaimType);
        if (perfilId.HasValue)
        {
            consulta = consulta.Where(x => x.RoleId == perfilId.Value);
        }

        var claims = await consulta.Select(x => new { x.RoleId, x.ClaimValue }).ToListAsync(cancellationToken);
        var ordem = Permissoes.Todas.Select((p, i) => (p.Codigo, i)).ToDictionary(x => x.Codigo, x => x.i);

        return claims
            .Where(x => Permissoes.Existe(x.ClaimValue))
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ClaimValue!).Distinct().OrderBy(x => ordem[x]).ToList());
    }

    private async Task<IReadOnlyList<(Guid Id, string Nome)>> ListarNomesAsync(CancellationToken cancellationToken) =>
        (await _context.Roles.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
        .Select(x => (x.Id, x.Name ?? string.Empty))
        .ToList();

    private static IReadOnlyCollection<string> Normalizar(IReadOnlyCollection<string>? permissoes) =>
        (permissoes ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();

    private static IdentityRoleClaim<Guid> NovaPermissao(Guid perfilId, string permissao) =>
        new() { RoleId = perfilId, ClaimType = Permissoes.ClaimType, ClaimValue = permissao };
}
