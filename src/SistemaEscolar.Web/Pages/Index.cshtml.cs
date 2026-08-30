using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
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

    public IndexModel(
        ILogger<IndexModel> logger,
        IAlunoService alunoService,
        IProfessorService professorService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService,
        ISerieService serieService,
        INotaService notaService)
    {
        _logger = logger;
        _alunoService = alunoService;
        _professorService = professorService;
        _turmaService = turmaService;
        _disciplinaService = disciplinaService;
        _serieService = serieService;
        _notaService = notaService;
    }

    public int TotalAlunos { get; private set; }
    public int TotalProfessores { get; private set; }
    public int TotalTurmas { get; private set; }
    public int TotalSeries { get; private set; }
    public int TotalDisciplinas { get; private set; }
    public int TotalPendenciasNota { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
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
    }
}
