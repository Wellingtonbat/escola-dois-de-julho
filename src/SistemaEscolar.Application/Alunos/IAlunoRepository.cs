using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Alunos;

public interface IAlunoRepository
{
    Task<IReadOnlyList<Aluno>> GetAllAsync(AlunoListFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Aluno?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string?> ObterMatriculaPorCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<int?> ObterMaiorOrdemSerieAnteriorAsync(string cpf, int anoLetivoAtual, CancellationToken cancellationToken = default);
    Task<bool> CpfMatriculadoNoAnoAsync(string cpf, int anoLetivo, Guid? ignoreId = null, CancellationToken cancellationToken = default);
    Task<string> ProximaMatriculaAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Aluno aluno, CancellationToken cancellationToken = default);
    Task UpdateAsync(Aluno aluno, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Aluno aluno, CancellationToken cancellationToken = default);
}
