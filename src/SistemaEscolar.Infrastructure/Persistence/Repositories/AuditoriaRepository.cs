using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Auditoria;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class AuditoriaRepository : IAuditoriaRepository
{
    private const string MarcaExcluidoSim = "\"IsDeleted\":true";
    private const string MarcaExcluidoNao = "\"IsDeleted\":false";

    private readonly ApplicationDbContext _context;

    public AuditoriaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<AuditLog> Itens, int Total)> ListarAsync(
        DateTime? deUtc,
        DateTime? ateUtcExclusivo,
        string? usuario,
        string? tabela,
        string? acao,
        int pular,
        int tomar,
        CancellationToken cancellationToken = default)
    {
        // IsDeleted = registro de ruído (sem alteração real) escondido pela migração de limpeza; nada é apagado.
        var query = _context.AuditLogs.AsNoTracking().Where(x => !x.IsDeleted);

        if (deUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= deUtc.Value);
        }

        if (ateUtcExclusivo.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc < ateUtcExclusivo.Value);
        }

        if (!string.IsNullOrWhiteSpace(tabela))
        {
            query = query.Where(x => x.TableName == tabela);
        }

        query = acao switch
        {
            "criado" => query.Where(x => x.Action == "ADDED"),
            "excluido" => query.Where(x => x.Action == "DELETED"
                || (x.Action == "MODIFIED" && x.NewValues != null && x.NewValues.Contains(MarcaExcluidoSim)
                    && x.OldValues != null && x.OldValues.Contains(MarcaExcluidoNao))),
            "alterado" => query.Where(x => x.Action == "MODIFIED"
                && !(x.NewValues != null && x.NewValues.Contains(MarcaExcluidoSim)
                    && x.OldValues != null && x.OldValues.Contains(MarcaExcluidoNao))),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(usuario))
        {
            var texto = usuario.Trim().ToLower();
            var digitos = new string(usuario.Where(char.IsDigit).ToArray());

            // Registros feitos por sessões antigas não guardam o nome: também procura pelo cadastro da conta.
            var idsPorNome = await _context.Users
                .AsNoTracking()
                .Where(x => x.FullName != null && x.FullName.ToLower().Contains(texto))
                .Select(x => x.Id.ToString())
                .ToListAsync(cancellationToken);

            query = query.Where(x =>
                (x.UserFullName != null && x.UserFullName.ToLower().Contains(texto))
                || (digitos.Length > 0 && x.UserName != null && x.UserName.Contains(digitos))
                || (x.CreatedBy != null && idsPorNome.Contains(x.CreatedBy)));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip(pular)
            .Take(tomar)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ObterNomesReferenciasAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var nomes = new Dictionary<Guid, string>();
        if (ids.Count == 0)
        {
            return nomes;
        }

        var lista = ids.ToList();

        var usuarios = await _context.Users.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.FullName ?? x.UserName })
            .ToListAsync(cancellationToken);
        var perfis = await _context.Roles.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.Name })
            .ToListAsync(cancellationToken);
        var alunos = await _context.Alunos.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.NomeCompleto })
            .ToListAsync(cancellationToken);
        var disciplinas = await _context.Disciplinas.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.Nome })
            .ToListAsync(cancellationToken);
        var professores = await _context.Professores.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.NomeCompleto })
            .ToListAsync(cancellationToken);
        var periodos = await _context.PeriodosLancamento.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.Descricao })
            .ToListAsync(cancellationToken);
        var turmas = await _context.Turmas.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.Nome })
            .ToListAsync(cancellationToken);
        var series = await _context.Series.AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .Select(x => new { x.Id, Nome = x.Nome })
            .ToListAsync(cancellationToken);

        foreach (var item in usuarios) { nomes[item.Id] = item.Nome ?? string.Empty; }
        foreach (var item in perfis) { nomes[item.Id] = item.Nome ?? string.Empty; }
        foreach (var item in alunos) { nomes[item.Id] = item.Nome; }
        foreach (var item in disciplinas) { nomes[item.Id] = item.Nome; }
        foreach (var item in professores) { nomes[item.Id] = item.Nome; }
        foreach (var item in periodos) { nomes[item.Id] = item.Nome; }
        foreach (var item in turmas) { nomes[item.Id] = item.Nome; }
        foreach (var item in series) { nomes[item.Id] = item.Nome; }

        return nomes;
    }
}
