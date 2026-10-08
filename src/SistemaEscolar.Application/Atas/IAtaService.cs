namespace SistemaEscolar.Application.Atas;

public interface IAtaService
{
    Task<IReadOnlyList<AtaListItemDto>> ListarAsync(int? anoLetivo = null, CancellationToken cancellationToken = default);

    // Turmas do ano letivo informado que ainda não têm Ata — para o seletor de "Nova Ata".
    Task<IReadOnlyList<AtaTurmaDisponivelDto>> ListarTurmasSemAtaAsync(int anoLetivo, CancellationToken cancellationToken = default);

    Task<(AtaResult Result, Guid? AtaId)> CriarParaTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default);

    Task<AtaDetalheDto?> ObterDetalheAsync(Guid ataId, CancellationToken cancellationToken = default);

    Task<AtaResult> SalvarMatriculaAsync(Guid ataAlunoId, string? matriculaPrefeitura, CancellationToken cancellationToken = default);
    Task<AtaResult> DefinirRFManualAsync(Guid ataAlunoId, string rf, CancellationToken cancellationToken = default);

    Task<AtaResult> FinalizarAsync(Guid ataId, CancellationToken cancellationToken = default);
    Task<AtaResult> HabilitarEdicaoAsync(Guid ataId, CancellationToken cancellationToken = default);
}
