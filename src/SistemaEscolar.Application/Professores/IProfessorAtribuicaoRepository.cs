namespace SistemaEscolar.Application.Professores;

public interface IProfessorAtribuicaoRepository
{
    Task<IReadOnlyList<(Guid ProfessorId, Guid TurmaId, Guid DisciplinaId)>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Guid TurmaId, Guid DisciplinaId)>> GetByProfessorIdAsync(Guid professorId, CancellationToken cancellationToken = default);

    Task SubstituirAsync(Guid professorId, IReadOnlyList<ProfessorAtribuicaoInput> atribuicoes, CancellationToken cancellationToken = default);
}
