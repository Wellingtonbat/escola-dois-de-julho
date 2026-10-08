using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Atas;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class AtaRepository : IAtaRepository
{
    private readonly ApplicationDbContext _context;

    public AtaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Ata>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Atas
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Atas
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Ata?> GetByTurmaIdAsync(Guid turmaId, CancellationToken cancellationToken = default)
    {
        return await _context.Atas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TurmaId == turmaId && !x.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<AtaAluno>> GetItensAsync(Guid ataId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AtaAluno>()
            .AsNoTracking()
            .Where(x => x.AtaId == ataId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<AtaAluno?> GetItemByIdAsync(Guid ataAlunoId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AtaAluno>()
            .FirstOrDefaultAsync(x => x.Id == ataAlunoId && !x.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<AtaAlunoDisciplina>> GetDisciplinasAsync(
        IReadOnlyCollection<Guid> ataAlunoIds, CancellationToken cancellationToken = default)
    {
        if (ataAlunoIds.Count == 0)
        {
            return Array.Empty<AtaAlunoDisciplina>();
        }

        return await _context.Set<AtaAlunoDisciplina>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && ataAlunoIds.Contains(x.AtaAlunoId))
            .ToListAsync(cancellationToken);
    }

    public async Task CriarComItensAsync(
        Ata ata,
        IReadOnlyList<(AtaAluno Item, IReadOnlyList<AtaAlunoDisciplina> Disciplinas)> itens,
        CancellationToken cancellationToken = default)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(cancellationToken);

        _context.Atas.Add(ata);

        foreach (var (item, disciplinas) in itens)
        {
            item.AtaId = ata.Id;
            _context.Set<AtaAluno>().Add(item);

            foreach (var disciplina in disciplinas)
            {
                disciplina.AtaAlunoId = item.Id;
                _context.Set<AtaAlunoDisciplina>().Add(disciplina);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Ata ata, CancellationToken cancellationToken = default)
    {
        _context.Atas.Update(ata);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarItemAsync(AtaAluno item, CancellationToken cancellationToken = default)
    {
        _context.Set<AtaAluno>().Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SincronizarItensAsync(
        IReadOnlyList<AtaAluno> novos,
        IReadOnlyList<AtaAluno> atualizados,
        IReadOnlyList<AtaAluno> removidos,
        CancellationToken cancellationToken = default)
    {
        if (novos.Count == 0 && atualizados.Count == 0 && removidos.Count == 0)
        {
            return;
        }

        await using var transacao = await _context.Database.BeginTransactionAsync(cancellationToken);

        // Primeiro as exclusões lógicas (índice único AtaId+AlunoId só vale para não excluídos), depois as inclusões.
        foreach (var item in removidos)
        {
            item.IsDeleted = true;
            _context.Set<AtaAluno>().Update(item);
        }

        foreach (var item in atualizados)
        {
            _context.Set<AtaAluno>().Update(item);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.Set<AtaAluno>().AddRange(novos);
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }

    public async Task FinalizarComResultadosAsync(
        Ata ata,
        IReadOnlyList<AtaAluno> itens,
        IReadOnlyList<AtaAlunoDisciplina> disciplinas,
        CancellationToken cancellationToken = default)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(cancellationToken);

        var itemIds = itens.Select(x => x.Id).ToList();
        var antigas = await _context.Set<AtaAlunoDisciplina>()
            .Where(x => !x.IsDeleted && itemIds.Contains(x.AtaAlunoId))
            .ToListAsync(cancellationToken);
        foreach (var antiga in antigas)
        {
            antiga.IsDeleted = true;
        }

        foreach (var item in itens)
        {
            _context.Set<AtaAluno>().Update(item);
        }

        _context.Atas.Update(ata);
        await _context.SaveChangesAsync(cancellationToken);

        // Em um segundo SaveChanges para não esbarrar no índice único (AtaAlunoId+DisciplinaId) das antigas.
        _context.Set<AtaAlunoDisciplina>().AddRange(disciplinas);
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }
}
