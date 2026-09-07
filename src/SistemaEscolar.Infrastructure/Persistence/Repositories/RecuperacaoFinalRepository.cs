using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class RecuperacaoFinalRepository : IRecuperacaoFinalRepository
{
    private readonly ApplicationDbContext _context;

    public RecuperacaoFinalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SalvarAsync(Guid alunoId, Guid disciplinaId, int anoLetivo, decimal valor, CancellationToken cancellationToken = default)
    {
        var existente = await _context.RecuperacoesFinais
            .FirstOrDefaultAsync(
                x => !x.IsDeleted && x.AlunoId == alunoId && x.DisciplinaId == disciplinaId && x.AnoLetivo == anoLetivo,
                cancellationToken);

        if (existente is null)
        {
            _context.RecuperacoesFinais.Add(new RecuperacaoFinal
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoId,
                DisciplinaId = disciplinaId,
                AnoLetivo = anoLetivo,
                Valor = valor
            });
        }
        else
        {
            existente.Valor = valor;
            _context.RecuperacoesFinais.Update(existente);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
