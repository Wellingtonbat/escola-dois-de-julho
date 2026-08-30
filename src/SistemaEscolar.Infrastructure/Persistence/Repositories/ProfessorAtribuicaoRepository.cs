using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class ProfessorAtribuicaoRepository : IProfessorAtribuicaoRepository
{
    private readonly ApplicationDbContext _context;

    public ProfessorAtribuicaoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<(Guid ProfessorId, Guid TurmaId, Guid DisciplinaId)>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProfessorAtribuicoes
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new ValueTuple<Guid, Guid, Guid>(x.ProfessorId, x.TurmaId, x.DisciplinaId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Guid TurmaId, Guid DisciplinaId)>> GetByProfessorIdAsync(Guid professorId, CancellationToken cancellationToken = default)
    {
        return await _context.ProfessorAtribuicoes
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.ProfessorId == professorId)
            .Select(x => new ValueTuple<Guid, Guid>(x.TurmaId, x.DisciplinaId))
            .ToListAsync(cancellationToken);
    }

    public async Task SubstituirAsync(Guid professorId, IReadOnlyList<ProfessorAtribuicaoInput> atribuicoes, CancellationToken cancellationToken = default)
    {
        var existentes = await _context.ProfessorAtribuicoes
            .Where(x => !x.IsDeleted && x.ProfessorId == professorId)
            .ToListAsync(cancellationToken);

        foreach (var existente in existentes)
        {
            existente.IsDeleted = true;
        }

        foreach (var atribuicao in atribuicoes)
        {
            _context.ProfessorAtribuicoes.Add(new ProfessorAtribuicao
            {
                Id = Guid.NewGuid(),
                ProfessorId = professorId,
                TurmaId = atribuicao.TurmaId,
                DisciplinaId = atribuicao.DisciplinaId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
