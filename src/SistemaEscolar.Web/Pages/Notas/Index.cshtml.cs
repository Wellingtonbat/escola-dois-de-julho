using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Web.Pages.Notas;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly INotaService _notaService;
    private readonly IAlunoService _alunoService;
    private readonly IProfessorService _professorService;
    private readonly IPeriodoService _periodoService;
    private readonly ITurmaService _turmaService;
    private readonly IDisciplinaService _disciplinaService;

    public IndexModel(
        INotaService notaService,
        IAlunoService alunoService,
        IProfessorService professorService,
        IPeriodoService periodoService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService)
    {
        _notaService = notaService;
        _alunoService = alunoService;
        _professorService = professorService;
        _periodoService = periodoService;
        _turmaService = turmaService;
        _disciplinaService = disciplinaService;
    }

    public IReadOnlyList<NotaListItemDto> Notas { get; private set; } = Array.Empty<NotaListItemDto>();
    public IReadOnlyList<SelectListItem> Alunos { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Periodos { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = Array.Empty<int>();
    public LancamentoMassaModalVm LancamentoMassaModal { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public int? AnoLetivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Trimestre { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = PageSizeFixo;

    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSelectOptionsAsync(cancellationToken);

        bool? statusFilter = Status switch
        {
            "finalizadas" => true,
            "pendentes" => false,
            _ => null
        };

        var filter = new NotaListFilter(Busca, AnoLetivo, Trimestre, statusFilter);
        var notasFiltradas = await _notaService.ListarAsync(filter, cancellationToken);

        TotalCount = notasFiltradas.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Notas = notasFiltradas
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(
        Guid id,
        string? busca,
        string? status,
        int? anoLetivo,
        int? trimestre,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar status de notas.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivo, Trimestre = trimestre, PageNumber = pageNumber, PageSize = pageSize });
        }

        var updated = await _notaService.AlternarFinalizacaoAsync(id, cancellationToken);
        if (!updated)
        {
            TempData["ErrorMessage"] = "Lançamento de nota não encontrado para atualização de status.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivo, Trimestre = trimestre, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Status do lançamento atualizado com sucesso.";
        return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivo, Trimestre = trimestre, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid id,
        string? busca,
        string? status,
        int? anoLetivo,
        int? trimestre,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir notas.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivo, Trimestre = trimestre, PageNumber = pageNumber, PageSize = pageSize });
        }

        var deleted = await _notaService.ExcluirAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Lançamento de nota não encontrado para exclusão.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivo, Trimestre = trimestre, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Lançamento de nota excluído com sucesso.";
        return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivo, Trimestre = trimestre, PageNumber = pageNumber, PageSize = pageSize });
    }

    private async Task LoadSelectOptionsAsync(CancellationToken cancellationToken)
    {
        var escopo = IsProfessorOnly()
            ? await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken)
            : null;

        var alunos = await _alunoService.ListarAsync(null, cancellationToken);
        var alunosVisiveis = alunos.Where(x => x.IsAtivo);
        if (escopo is not null)
        {
            alunosVisiveis = alunosVisiveis.Where(x => x.TurmaId.HasValue && escopo.TurmaIds.Contains(x.TurmaId.Value));
        }

        Alunos = alunosVisiveis
            .OrderBy(x => x.NomeCompleto)
            .Select(x => new SelectListItem(x.NomeCompleto, x.Id.ToString()))
            .ToList();

        var periodos = await _periodoService.ListarAsync(null, cancellationToken);
        AnosDisponiveis = periodos
            .Select(x => x.AnoLetivo)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        Periodos = periodos
            .OrderByDescending(x => x.AnoLetivo)
            .ThenBy(x => x.Trimestre)
            .Select(x => new SelectListItem(x.Descricao, x.Id.ToString()))
            .ToList();

        var (turmasLancamentoMassa, turmaDisciplinaJson) = await LancamentoMassaOptions.BuildAsync(
            _professorService, _turmaService, _disciplinaService, escopo, cancellationToken);

        LancamentoMassaModal = new LancamentoMassaModalVm
        {
            Periodos = Periodos,
            Turmas = turmasLancamentoMassa,
            TurmaDisciplinaJson = turmaDisciplinaJson,
            MostrarAvisoEscopo = IsProfessorOnly(),
        };
    }

    private bool CanManageNotas()
    {
        return User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria") || User.IsInRole("Professor");
    }

    private bool IsProfessorOnly() =>
        User.IsInRole("Professor")
        && !User.IsInRole("Diretor")
        && !User.IsInRole("Coordenador")
        && !User.IsInRole("Cordenador")
        && !User.IsInRole("Secretaria");
}
