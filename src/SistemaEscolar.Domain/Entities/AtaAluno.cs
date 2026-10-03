using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Entities;

// Linha de um aluno dentro de uma Ata. TP e MC são recalculados a partir das disciplinas (AtaAlunoDisciplina)
// enquanto a Ata está em Rascunho; ao Finalizar, os valores ficam congelados (deixam de ser recalculados).
public sealed class AtaAluno : BaseEntity
{
    public Guid Id { get; set; }
    public Guid AtaId { get; set; }
    public Guid AlunoId { get; set; }

    // TP = soma das médias finais das disciplinas; MC = TP / número de disciplinas.
    public decimal TP { get; set; }
    public decimal MC { get; set; }

    // Avaliação Final: só é preenchida quando alguma disciplina ficou abaixo da média. Null = ainda não lançada.
    public decimal? AvF { get; set; }

    // "Pendente" | "Aprovado" | "Conservado" | "Transferido" — ver AtaConstantes.
    public string RF { get; set; } = string.Empty;
}
