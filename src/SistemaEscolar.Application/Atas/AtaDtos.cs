namespace SistemaEscolar.Application.Atas;

public sealed record AtaResult(bool Succeeded, string? ErrorMessage = null)
{
    public static AtaResult Success() => new(true);

    public static AtaResult Fail(string errorMessage) => new(false, errorMessage);
}

public sealed record AtaListItemDto(
    Guid Id,
    Guid TurmaId,
    string TurmaNome,
    string SerieNome,
    int AnoLetivo,
    string Status,
    int TotalAlunos,
    int AlunosComResultado,
    DateTime? FinalizadaEmUtc,
    string? FinalizadaPorNome)
{
    public bool TodosComResultado => TotalAlunos > 0 && AlunosComResultado == TotalAlunos;
}

// Turma do ano letivo corrente que ainda não tem Ata — alimenta o seletor de "Nova Ata".
public sealed record AtaTurmaDisponivelDto(Guid TurmaId, string TurmaNome, string SerieNome, int AnoLetivo);

public sealed record AtaAlunoDisciplinaDto(Guid DisciplinaId, string DisciplinaNome, string DisciplinaCodigo, decimal ResultadoFinalAno);

// Coluna da grade/PDF da Ata: uma por disciplina da série, ordenada por nome.
public sealed record AtaDisciplinaColunaDto(Guid DisciplinaId, string Nome, string Codigo);

public sealed record AtaAlunoItemDto(
    Guid AtaAlunoId,
    Guid AlunoId,
    int Numero,
    string NomeCompleto,
    string? MatriculaPrefeitura,
    bool AlunoAtivo,
    // Vazio para Transferido(a)/Deixou de frequentar: na Ata oficial a linha sai sem notas.
    IReadOnlyList<AtaAlunoDisciplinaDto> Disciplinas,
    string RF,
    // O que ainda falta para o resultado sair de "Pendente" (ex.: "Avaliação Final de Matemática").
    string? Pendencia,
    // Série que o aluno está apto a cursar no ano seguinte; nulo quando não se aplica ou fica em branco.
    string? AptoACursar)
{
    public string RFDescricao => AtaConstantes.DescreverRF(RF);
    public bool RFManual => AtaConstantes.EhRFManual(RF);
}

public sealed record AtaDetalheDto(
    Guid AtaId,
    Guid TurmaId,
    string TurmaNome,
    string SerieNome,
    int AnoLetivo,
    string Turno,
    string? DiretorNome,
    string Status,
    bool PodeEditar,
    DateTime? FinalizadaEmUtc,
    string? FinalizadaPorNome,
    IReadOnlyList<AtaDisciplinaColunaDto> DisciplinasColunas,
    IReadOnlyList<AtaAlunoItemDto> Itens)
{
    public int TotalAlunos => Itens.Count;
    public int AlunosComResultado => Itens.Count(x => x.RF != AtaConstantes.RFPendente);
    public bool TodosComResultado => TotalAlunos > 0 && AlunosComResultado == TotalAlunos;
}
