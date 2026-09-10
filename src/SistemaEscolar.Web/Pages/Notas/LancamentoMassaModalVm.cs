using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolar.Web.Pages.Notas;

public sealed class LancamentoMassaModalVm
{
    public IReadOnlyList<SelectListItem> Periodos { get; init; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Turmas { get; init; } = Array.Empty<SelectListItem>();
    public string TurmaDisciplinaJson { get; init; } = "[]";
    public bool MostrarAvisoEscopo { get; init; }
    public Guid? PeriodoSelecionadoId { get; init; }
    public Guid? TurmaSelecionadaId { get; init; }
    public Guid? DisciplinaSelecionadaId { get; init; }
    public string? DisciplinaSelecionadaNome { get; init; }
}
