using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class AlunoRepository : IAlunoRepository
{
    private readonly ApplicationDbContext _context;

    public AlunoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Aluno>> GetAllAsync(AlunoListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Alunos
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Serie))
        {
            var serieNome = filter.Serie.Trim();
            var serieIds = await _context.Set<Serie>()
                .AsNoTracking()
                .Where(s => s.Nome == serieNome)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(x => serieIds.Contains(x.SerieId));
        }

        if (!string.IsNullOrWhiteSpace(filter?.Busca))
        {
            var busca = filter.Busca.Trim().ToLower();
            var turmaIdsBusca = _context.Set<Turma>()
                .AsNoTracking()
                .Where(t => t.Nome.ToLower().Contains(busca))
                .Select(t => t.Id);

            query = query.Where(x =>
                x.NomeCompleto.ToLower().Contains(busca) ||
                x.Matricula.ToLower().Contains(busca) ||
                x.Cpf.Contains(busca) ||
                (x.TurmaId.HasValue && turmaIdsBusca.Contains(x.TurmaId.Value)));
        }

        if (filter?.TurmaId.HasValue == true)
        {
            query = query.Where(x => x.TurmaId == filter.TurmaId.Value);
        }

        if (filter?.IsAtivo.HasValue == true)
        {
            query = query.Where(x => x.IsAtivo == filter.IsAtivo.Value);
        }

        return await query
            .OrderBy(x => x.NomeCompleto)
            .ToListAsync(cancellationToken);
    }

    public async Task<Aluno?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Alunos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<string?> ObterMatriculaPorCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Alunos
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Cpf == cpf)
            .OrderByDescending(x => x.AnoLetivo)
            .Select(x => x.Matricula)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ObterMaiorOrdemSerieAnteriorAsync(string cpf, int anoLetivoAtual, CancellationToken cancellationToken = default)
    {
        var ordens = await _context.Alunos
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Cpf == cpf && x.AnoLetivo < anoLetivoAtual)
            .Join(_context.Set<Serie>(), aluno => aluno.SerieId, serie => serie.Id, (aluno, serie) => serie.Ordem)
            .ToListAsync(cancellationToken);

        return ordens.Count == 0 ? null : ordens.Max();
    }

    public async Task<bool> CpfMatriculadoNoAnoAsync(string cpf, int anoLetivo, Guid? ignoreId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Alunos
            .AsNoTracking()
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.Cpf == cpf &&
                x.AnoLetivo == anoLetivo &&
                (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
    }

    public async Task<string> ProximaMatriculaAsync(CancellationToken cancellationToken = default)
    {
        var proximoValor = await _context.Database
            .SqlQuery<long>($"SELECT nextval('\"AlunoMatriculaSequence\"') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return proximoValor.ToString();
    }

    public async Task AddAsync(Aluno aluno, CancellationToken cancellationToken = default)
    {
        _context.Alunos.Add(aluno);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Aluno aluno, CancellationToken cancellationToken = default)
    {
        _context.Alunos.Update(aluno);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Aluno aluno, CancellationToken cancellationToken = default)
    {
        aluno.IsDeleted = true;
        _context.Alunos.Update(aluno);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
