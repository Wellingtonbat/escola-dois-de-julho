using System.Globalization;
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

public sealed class LancamentoMassaModel : PageModel
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IAlunoService _alunoService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly ITurmaService _turmaService;
    private readonly IProfessorService _professorService;
    private readonly IPeriodoService _periodoService;
    private readonly INotaService _notaService;

    public LancamentoMassaModel(
        IAlunoService alunoService,
        IDisciplinaService disciplinaService,
        ITurmaService turmaService,
        IProfessorService professorService,
        IPeriodoService periodoService,
        INotaService notaService)
    {
        _alunoService = alunoService;
        _disciplinaService = disciplinaService;
        _turmaService = turmaService;
        _professorService = professorService;
        _periodoService = periodoService;
        _notaService = notaService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TurmaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid DisciplinaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid PeriodoId { get; set; }

    public TurmaListItemDto? Turma { get; private set; }
    public DisciplinaListItemDto? Disciplina { get; private set; }
    public PeriodoListItemDto? Periodo { get; private set; }
    public Guid? ProfessorId { get; private set; }
    public bool PodeEditar { get; private set; }
    public IReadOnlyList<AlunoLinhaVm> Alunos { get; private set; } = Array.Empty<AlunoLinhaVm>();
    public int TotalAlunos { get; private set; }
    public int LancadosCount { get; private set; }
    public int PendentesCount { get; private set; }
    public LancamentoMassaModalVm LancamentoMassaModal { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para lançar notas.";
            return RedirectToPage("/Notas/Index");
        }

        var turma = await _turmaService.ObterPorIdAsync(TurmaId, cancellationToken);
        var disciplina = (await _disciplinaService.ListarAsync(null, cancellationToken)).FirstOrDefault(x => x.Id == DisciplinaId);
        var periodo = await _periodoService.ObterPorIdAsync(PeriodoId, cancellationToken);

        if (turma is null || disciplina is null || periodo is null)
        {
            TempData["ErrorMessage"] = "Turma, disciplina ou trimestre não encontrado.";
            return RedirectToPage("/Notas/Index");
        }

        var escopo = IsProfessorOnly()
            ? await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken)
            : null;

        if (escopo is not null && (!escopo.TurmaIds.Contains(TurmaId) || !escopo.DisciplinaIds.Contains(DisciplinaId)))
        {
            TempData["ErrorMessage"] = "Você não está vinculado a esta turma ou disciplina.";
            return RedirectToPage("/Notas/Index");
        }

        Turma = turma;
        Disciplina = disciplina;
        Periodo = periodo;

        var todosPeriodos = await _periodoService.ListarAsync(null, cancellationToken);
        var periodosOptions = todosPeriodos
            .OrderByDescending(x => x.AnoLetivo)
            .ThenBy(x => x.Trimestre)
            .Select(x => new SelectListItem(x.Descricao, x.Id.ToString()))
            .ToList();

        var (turmasModal, turmaDisciplinaJson) = await LancamentoMassaOptions.BuildAsync(
            _professorService, _turmaService, _disciplinaService, escopo, cancellationToken);

        LancamentoMassaModal = new LancamentoMassaModalVm
        {
            Periodos = periodosOptions,
            Turmas = turmasModal,
            TurmaDisciplinaJson = turmaDisciplinaJson,
            MostrarAvisoEscopo = IsProfessorOnly(),
            PeriodoSelecionadoId = PeriodoId,
            TurmaSelecionadaId = TurmaId,
            DisciplinaSelecionadaId = DisciplinaId,
            DisciplinaSelecionadaNome = disciplina.Nome,
        };

        var todosProfessores = await _professorService.ListarAsync(null, cancellationToken);
        ProfessorId = escopo?.ProfessorId ?? todosProfessores
            .Where(p => p.IsAtivo)
            .SelectMany(p => p.Atribuicoes.Select(a => new { p.Id, a.TurmaId, a.DisciplinaId }))
            .Where(x => x.TurmaId == TurmaId && x.DisciplinaId == DisciplinaId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefault();

        var hoje = DateTime.UtcNow;
        PodeEditar = PeriodoDisponibilidade.EstaAberto(periodo, hoje) || User.IsInRole("Diretor");

        var alunosDaTurma = (await _alunoService.ListarAsync(new AlunoListFilter(null, null, TurmaId, true), cancellationToken))
            .OrderBy(x => x.NomeCompleto)
            .ToList();

        var notasDaTurma = (await _notaService.ListarAsync(null, cancellationToken))
            .Where(x => x.DisciplinaId == DisciplinaId && x.PeriodoLancamentoId == PeriodoId)
            .ToList();

        Alunos = alunosDaTurma.Select(aluno =>
        {
            var nota = notasDaTurma.FirstOrDefault(n => n.AlunoId == aluno.Id);
            var calc = NotaCalculo.Calcular(nota);

            return new AlunoLinhaVm(
                aluno.Id,
                aluno.NomeCompleto,
                aluno.Matricula,
                nota?.Id,
                FormatarEntrada(nota?.Avaliacao1),
                FormatarEntrada(nota?.Avaliacao2),
                FormatarEntrada(nota?.Avaliacao3),
                FormatarEntrada(nota?.RecuperacaoParalela),
                FormatarExibicao(calc.Soma),
                FormatarExibicao(calc.Final),
                calc.PodeRecuperar,
                calc.StatusLabel,
                calc.StatusClass);
        }).ToList();

        TotalAlunos = Alunos.Count;
        LancadosCount = Alunos.Count(x => x.StatusLabel != "Não lançado");
        PendentesCount = TotalAlunos - LancadosCount;

        return Page();
    }

    public async Task<IActionResult> OnPostSalvarAsync(
        Guid alunoId,
        Guid disciplinaId,
        Guid? professorId,
        Guid periodoLancamentoId,
        string? avaliacao1,
        string? avaliacao2,
        string? avaliacao3,
        string? recuperacaoParalela,
        CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            return new JsonResult(new { succeeded = false, error = "Você não tem permissão para lançar notas." });
        }

        if (!TryParseNota(avaliacao1, out var av1)
            || !TryParseNota(avaliacao2, out var av2)
            || !TryParseNota(avaliacao3, out var av3)
            || !TryParseNota(recuperacaoParalela, out var rec))
        {
            return new JsonResult(new { succeeded = false, error = "Formato de nota inválido. Use valores numéricos entre 0 e 10." });
        }

        var escopo = IsProfessorOnly()
            ? await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken)
            : null;

        if (professorId is null && escopo is null)
        {
            return new JsonResult(new { succeeded = false, error = "Não há professor atribuído a esta disciplina para a turma do aluno." });
        }

        var notaExistente = (await _notaService.ListarAsync(null, cancellationToken))
            .FirstOrDefault(n => n.AlunoId == alunoId && n.DisciplinaId == disciplinaId && n.PeriodoLancamentoId == periodoLancamentoId);

        var request = new NotaCreateRequest(
            alunoId,
            disciplinaId,
            professorId ?? escopo!.ProfessorId,
            periodoLancamentoId,
            av1,
            av2,
            av3,
            rec,
            notaExistente?.IsFinalizada ?? false);

        var resultado = notaExistente is null
            ? await _notaService.CriarAsync(request, cancellationToken)
            : await _notaService.AtualizarAsync(notaExistente.Id, request, cancellationToken);

        if (!resultado.Succeeded)
        {
            return new JsonResult(new { succeeded = false, error = resultado.ErrorMessage ?? "Não foi possível salvar a nota." });
        }

        var notaAtualizada = (await _notaService.ListarAsync(null, cancellationToken))
            .FirstOrDefault(n => n.AlunoId == alunoId && n.DisciplinaId == disciplinaId && n.PeriodoLancamentoId == periodoLancamentoId);
        var calc = NotaCalculo.Calcular(notaAtualizada);

        return new JsonResult(new
        {
            succeeded = true,
            soma = FormatarExibicao(calc.Soma),
            final = FormatarExibicao(calc.Final),
            podeRecuperar = calc.PodeRecuperar,
            statusLabel = calc.StatusLabel,
            statusClass = calc.StatusClass,
        });
    }

    private bool CanManageNotas() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria") || User.IsInRole("Professor");

    private bool IsProfessorOnly() =>
        User.IsInRole("Professor")
        && !User.IsInRole("Diretor")
        && !User.IsInRole("Coordenador")
        && !User.IsInRole("Cordenador")
        && !User.IsInRole("Secretaria");

    private static string FormatarEntrada(decimal? valor) =>
        valor.HasValue ? valor.Value.ToString("0.##", PtBr) : string.Empty;

    private static string FormatarExibicao(decimal? valor) =>
        valor.HasValue ? valor.Value.ToString("0.00", PtBr) : "–";

    private static bool TryParseNota(string? rawValue, out decimal? value)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            return true;
        }

        var text = rawValue.Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(text, NumberStyles.Number, PtBr, out parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    public sealed record AlunoLinhaVm(
        Guid AlunoId,
        string NomeCompleto,
        string Matricula,
        Guid? NotaId,
        string Avaliacao1,
        string Avaliacao2,
        string Avaliacao3,
        string RecuperacaoParalela,
        string Soma,
        string ResultadoFinal,
        bool PodeRecuperar,
        string StatusLabel,
        string StatusClass);
}
