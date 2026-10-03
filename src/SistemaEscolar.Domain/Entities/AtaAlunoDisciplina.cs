using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

// Resultado final de uma disciplina para um aluno dentro de uma Ata (mesmo valor que já aparece em
// Resultados, incluindo Recuperação Final por disciplina quando houver). Guarda o nome da disciplina junto
// porque, depois que a Ata é finalizada, o registro vira histórico — não deve mudar se a disciplina for
// renomeada ou desativada depois.
public sealed class AtaAlunoDisciplina : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AtaAlunoId { get; set; }
    public Guid DisciplinaId { get; set; }
    public string DisciplinaNome { get; set; } = string.Empty;
    public string DisciplinaCodigo { get; set; } = string.Empty;
    public decimal ResultadoFinalAno { get; set; }
}
