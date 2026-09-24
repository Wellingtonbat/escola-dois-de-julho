using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Auditoria;

public interface IAuditoriaRepository
{
    // Datas em UTC. "acao" já vem normalizada: criado, alterado ou excluido.
    Task<(IReadOnlyList<AuditLog> Itens, int Total)> ListarAsync(
        DateTime? deUtc,
        DateTime? ateUtcExclusivo,
        string? usuario,
        string? tabela,
        string? acao,
        int pular,
        int tomar,
        CancellationToken cancellationToken = default);

    // Nomes legíveis para ids citados nos registros: usuários, perfis, alunos, disciplinas,
    // professores, períodos, turmas e séries.
    Task<IReadOnlyDictionary<Guid, string>> ObterNomesReferenciasAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
