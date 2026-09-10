using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Dashboard;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IAlunoService _alunoService;
    private readonly IProfessorService _professorService;
    private readonly ITurmaService _turmaService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly ISerieService _serieService;
    private readonly INotaService _notaService;
    private readonly IPeriodoService _periodoService;
    private readonly IDashboardService _dashboardService;

    public IndexModel(
        ILogger<IndexModel> logger,
        IAlunoService alunoService,
        IProfessorService professorService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService,
        ISerieService serieService,
        INotaService notaService,
        IPeriodoService periodoService,
        IDashboardService dashboardService)
    {
        _logger = logger;
        _alunoService = alunoService;
        _professorService = professorService;
        _turmaService = turmaService;
        _disciplinaService = disciplinaService;
        _serieService = serieService;
        _notaService = notaService;
        _periodoService = periodoService;
        _dashboardService = dashboardService;
    }

    [BindProperty(SupportsGet = true)]
    public int? AnoLetivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Trimestre { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ProfessorId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? TurmaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DisciplinaId { get; set; }

    public bool PodeVerDashboardCompleto { get; private set; }

    public int TotalAlunos { get; private set; }
    public int TotalProfessores { get; private set; }
    public int TotalTurmas { get; private set; }
    public int TotalSeries { get; private set; }
    public int TotalDisciplinas { get; private set; }
    public int TotalPendenciasNota { get; private set; }

    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = Array.Empty<int>();
    public IReadOnlyList<SelectListItem> Professores { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Turmas { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Disciplinas { get; private set; } = Array.Empty<SelectListItem>();
    public DashboardDadosDto? Dados { get; private set; }

    public string DadosIniciaisJson => Dados is null
        ? "null"
        : JsonSerializer.Serialize(Dados, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PodeVerDashboardCompleto = CanVerDashboardCompleto();

        if (!PodeVerDashboardCompleto)
        {
            var alunos = await _alunoService.ListarAsync(null, cancellationToken);
            var professores = await _professorService.ListarAsync(null, cancellationToken);
            var turmas = await _turmaService.ListarAsync(null, cancellationToken);
            var disciplinas = await _disciplinaService.ListarAsync(null, cancellationToken);
            var series = await _serieService.ListarAsync(null, cancellationToken);
            var notasPendentes = await _notaService.ListarAsync(new NotaListFilter(null, null, null, false), cancellationToken);

            TotalAlunos = alunos.Count;
            TotalProfessores = professores.Count;
            TotalTurmas = turmas.Count;
            TotalDisciplinas = disciplinas.Count;
            TotalSeries = series.Count;
            TotalPendenciasNota = notasPendentes.Count;
            return;
        }

        var periodos = await _periodoService.ListarAsync(null, cancellationToken);
        AnosDisponiveis = periodos.Select(p => p.AnoLetivo).Distinct().OrderByDescending(x => x).ToList();
        var anoLetivo = AnoLetivo ?? AnosDisponiveis.FirstOrDefault(DateTime.Now.Year);

        Professores = (await _professorService.ListarAsync(null, cancellationToken))
            .Where(p => p.IsAtivo)
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new SelectListItem(p.NomeCompleto, p.Id.ToString()))
            .ToList();

        Turmas = (await _turmaService.ListarAsync(new TurmaListFilter(null, null, null, true), cancellationToken))
            .OrderBy(t => t.Nome)
            .Select(t => new SelectListItem($"{t.Nome} ({t.SerieNome})", t.Id.ToString()))
            .ToList();

        Disciplinas = (await _disciplinaService.ListarAsync(null, cancellationToken))
            .Where(d => d.IsAtiva)
            .OrderBy(d => d.Nome)
            .Select(d => new SelectListItem(d.Nome, d.Id.ToString()))
            .ToList();

        var filtro = new DashboardFiltroDto(anoLetivo, Trimestre, ProfessorId, TurmaId, DisciplinaId);
        Dados = await _dashboardService.ObterDadosAsync(filtro, cancellationToken);
    }

    public async Task<IActionResult> OnGetDadosAsync(
        int anoLetivo,
        int? trimestre,
        Guid? professorId,
        Guid? turmaId,
        Guid? disciplinaId,
        CancellationToken cancellationToken)
    {
        if (!CanVerDashboardCompleto())
        {
            return Forbid();
        }

        var filtro = new DashboardFiltroDto(anoLetivo, trimestre, professorId, turmaId, disciplinaId);
        var dados = await _dashboardService.ObterDadosAsync(filtro, cancellationToken);
        return new JsonResult(dados);
    }

    private bool CanVerDashboardCompleto() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");
}
