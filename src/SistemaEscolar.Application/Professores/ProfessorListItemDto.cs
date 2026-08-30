namespace SistemaEscolar.Application.Professores;

public sealed record ProfessorListItemDto(
    Guid Id,
    string NomeCompleto,
    string Email,
    string UsuarioCpf,
    IReadOnlyList<ProfessorAtribuicaoDto> Atribuicoes,
    bool IsAtivo)
{
    public string DisciplinasResumo => Resumo(x => x.DisciplinaNome);

    public string SeriesResumo => Resumo(x => x.SerieNome);

    public string TurmasResumo => Resumo(x => x.TurmaNome);

    private string Resumo(Func<ProfessorAtribuicaoDto, string> selector) =>
        string.Join(", ", Atribuicoes.Select(selector).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
}
