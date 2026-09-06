using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;

namespace SistemaEscolar.Web.Pages.Notas;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly INotaService _notaService;
    private readonly IAlunoService _alunoService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IProfessorService _professorService;
    private readonly IPeriodoService _periodoService;

    public IndexModel(
        INotaService notaService,
        IAlunoService alunoService,
        IDisciplinaService disciplinaService,
        IProfessorService professorService,
        IPeriodoService periodoService)
    {
        _notaService = notaService;
        _alunoService = alunoService;
        _disciplinaService = disciplinaService;
        _professorService = professorService;
        _periodoService = periodoService;
    }

    public IReadOnlyList<NotaListItemDto> Notas { get; private set; } = Array.Empty<NotaListItemDto>();
    public IReadOnlyList<SelectListItem> Alunos { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Disciplinas { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Professores { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Periodos { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = Array.Empty<int>();

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

    public async Task<IActionResult> OnPostCreateAsync(
        Guid alunoId,
        Guid disciplinaId,
        Guid professorId,
        Guid periodoLancamentoId,
        string? avaliacao1,
        string? avaliacao2,
        string? avaliacao3,
        string? recuperacaoParalela,
        bool isFinalizada,
        string? busca,
        string? status,
        int? anoLetivoFilter,
        int? trimestreFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para lançar notas.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (!TryParseNota(avaliacao1, out var avaliacao1Value)
            || !TryParseNota(avaliacao2, out var avaliacao2Value)
            || !TryParseNota(avaliacao3, out var avaliacao3Value)
            || !TryParseNota(recuperacaoParalela, out var recuperacaoParalelaValue))
        {
            TempData["ErrorMessage"] = "Formato de nota inválido. Use valores numéricos entre 0 e 10.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _notaService.CriarAsync(
            new NotaCreateRequest(alunoId, disciplinaId, professorId, periodoLancamentoId, avaliacao1Value, avaliacao2Value, avaliacao3Value, recuperacaoParalelaValue, isFinalizada),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível lançar a nota.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Lançamento de nota cadastrado com sucesso.";
        return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        Guid alunoId,
        Guid disciplinaId,
        Guid professorId,
        Guid periodoLancamentoId,
        string? avaliacao1,
        string? avaliacao2,
        string? avaliacao3,
        string? recuperacaoParalela,
        bool isFinalizada,
        string? busca,
        string? status,
        int? anoLetivoFilter,
        int? trimestreFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar notas.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (!TryParseNota(avaliacao1, out var avaliacao1Value)
            || !TryParseNota(avaliacao2, out var avaliacao2Value)
            || !TryParseNota(avaliacao3, out var avaliacao3Value)
            || !TryParseNota(recuperacaoParalela, out var recuperacaoParalelaValue))
        {
            TempData["ErrorMessage"] = "Formato de nota inválido. Use valores numéricos entre 0 e 10.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _notaService.AtualizarAsync(
            id,
            new NotaCreateRequest(alunoId, disciplinaId, professorId, periodoLancamentoId, avaliacao1Value, avaliacao2Value, avaliacao3Value, recuperacaoParalelaValue, isFinalizada),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível atualizar a nota.";
            return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Lançamento de nota atualizado com sucesso.";
        return RedirectToPage("/Notas/Index", new { Busca = busca, Status = status, AnoLetivo = anoLetivoFilter, Trimestre = trimestreFilter, PageNumber = pageNumber, PageSize = pageSize });
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

        var disciplinas = await _disciplinaService.ListarAsync(null, cancellationToken);
        var disciplinasVisiveis = disciplinas.Where(x => x.IsAtiva);
        if (escopo is not null)
        {
            disciplinasVisiveis = disciplinasVisiveis.Where(x => escopo.DisciplinaIds.Contains(x.Id));
        }

        Disciplinas = disciplinasVisiveis
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem($"{x.Nome} ({x.Codigo})", x.Id.ToString()))
            .ToList();

        var professores = await _professorService.ListarAsync(null, cancellationToken);
        var professoresVisiveis = professores.Where(x => x.IsAtivo);
        if (escopo is not null)
        {
            professoresVisiveis = professoresVisiveis.Where(x => x.Id == escopo.ProfessorId);
        }

        Professores = professoresVisiveis
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

    private static bool TryParseNota(string? rawValue, out decimal? value)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            return true;
        }

        var text = rawValue.Trim();

        // <input type="number"> always submits values with '.' as the decimal separator,
        // regardless of browser locale (HTML spec), so InvariantCulture must be tried first.
        // pt-BR is kept as a fallback for any manually-typed comma-decimal input.
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }
}
