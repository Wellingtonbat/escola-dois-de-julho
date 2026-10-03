using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Atas;

public interface IAtaRepository
{
    Task<IReadOnlyList<Ata>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Ata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Ata?> GetByTurmaIdAsync(Guid turmaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AtaAluno>> GetItensAsync(Guid ataId, CancellationToken cancellationToken = default);
    Task<AtaAluno?> GetItemByIdAsync(Guid ataAlunoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtaAlunoDisciplina>> GetDisciplinasAsync(IReadOnlyCollection<Guid> ataAlunoIds, CancellationToken cancellationToken = default);

    // Cria a Ata já com todos os itens (um por aluno) e as disciplinas de cada item, em uma única operação.
    Task CriarComItensAsync(
        Ata ata,
        IReadOnlyList<(AtaAluno Item, IReadOnlyList<AtaAlunoDisciplina> Disciplinas)> itens,
        CancellationToken cancellationToken = default);

    Task AtualizarAsync(Ata ata, CancellationToken cancellationToken = default);
    Task AtualizarItemAsync(AtaAluno item, CancellationToken cancellationToken = default);
}
