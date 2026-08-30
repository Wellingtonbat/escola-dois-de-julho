using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class NotaRepository : INotaRepository
{
    private readonly ApplicationDbContext _context;

    public NotaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NotaListItemDto>> GetAllAsync(NotaListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query =
            from n in _context.Notas.AsNoTracking()
            join a in _context.Alunos.AsNoTracking() on n.AlunoId equals a.Id
            join d in _context.Disciplinas.AsNoTracking() on n.DisciplinaId equals d.Id
            join p in _context.Professores.AsNoTracking() on n.ProfessorId equals p.Id
            join periodo in _context.PeriodosLancamento.AsNoTracking() on n.PeriodoLancamentoId equals periodo.Id
            where !n.IsDeleted && !a.IsDeleted && !d.IsDeleted && !p.IsDeleted
            select new
            {
                n.Id,
                n.AlunoId,
                AlunoNome = a.NomeCompleto,
                n.DisciplinaId,
                DisciplinaNome = d.Nome,
                n.ProfessorId,
                ProfessorNome = p.NomeCompleto,
                n.PeriodoLancamentoId,
                PeriodoDescricao = periodo.Descricao,
                AnoLetivo = periodo.AnoLetivo,
                Trimestre = periodo.Bimestre,
                n.Avaliacao1,
                n.Avaliacao2,
                n.Avaliacao3,
                n.RecuperacaoParalela,
                n.ResultadoUnidade,
                n.ResultadoFinalUnidade,
                n.IsFinalizada
            };

        if (!string.IsNullOrWhiteSpace(filter?.Busca))
        {
            var busca = filter.Busca.Trim();
            query = query.Where(x =>
                x.AlunoNome.Contains(busca) ||
                x.DisciplinaNome.Contains(busca) ||
                x.ProfessorNome.Contains(busca));
        }

        if (filter?.AnoLetivo.HasValue == true)
        {
            query = query.Where(x => x.AnoLetivo == filter.AnoLetivo.Value);
        }

        if (filter?.Trimestre.HasValue == true)
        {
            query = query.Where(x => x.Trimestre == filter.Trimestre.Value);
        }

        if (filter?.IsFinalizada.HasValue == true)
        {
            query = query.Where(x => x.IsFinalizada == filter.IsFinalizada.Value);
        }

        return await query
            .OrderByDescending(x => x.AnoLetivo)
            .ThenBy(x => x.Trimestre)
            .ThenBy(x => x.DisciplinaNome)
            .ThenBy(x => x.AlunoNome)
            .Select(x => new NotaListItemDto(
                x.Id,
                x.AlunoId,
                x.AlunoNome,
                x.DisciplinaId,
                x.DisciplinaNome,
                x.ProfessorId,
                x.ProfessorNome,
                x.PeriodoLancamentoId,
                x.PeriodoDescricao,
                x.AnoLetivo,
                x.Trimestre,
                x.Avaliacao1,
                x.Avaliacao2,
                x.Avaliacao3,
                x.RecuperacaoParalela,
                x.ResultadoUnidade,
                x.ResultadoFinalUnidade,
                x.IsFinalizada))
            .ToListAsync(cancellationToken);
    }

    public Task<Nota?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Notas
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public Task<bool> LancamentoDuplicadoExisteAsync(
        Guid alunoId,
        Guid disciplinaId,
        Guid periodoLancamentoId,
        Guid? ignoreId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.Notas
            .AsNoTracking()
            .AnyAsync(x =>
                !x.IsDeleted
                && x.AlunoId == alunoId
                && x.DisciplinaId == disciplinaId
                && x.PeriodoLancamentoId == periodoLancamentoId
                && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
    }

    public Task<bool> TemPendenciasAsync(Guid periodoLancamentoId, CancellationToken cancellationToken = default)
    {
        return _context.Notas
            .AsNoTracking()
            .AnyAsync(
                x => !x.IsDeleted
                     && x.PeriodoLancamentoId == periodoLancamentoId
                     && !x.IsFinalizada,
                cancellationToken);
    }

    public async Task AddAsync(Nota nota, CancellationToken cancellationToken = default)
    {
        _context.Notas.Add(nota);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Nota nota, CancellationToken cancellationToken = default)
    {
        _context.Notas.Update(nota);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Nota nota, CancellationToken cancellationToken = default)
    {
        nota.IsDeleted = true;
        _context.Notas.Update(nota);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
