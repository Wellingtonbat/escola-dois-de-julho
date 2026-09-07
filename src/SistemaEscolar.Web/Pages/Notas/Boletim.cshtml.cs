using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;

namespace SistemaEscolar.Web.Pages.Notas;

public sealed class BoletimModel : PageModel
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IAlunoService _alunoService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IProfessorService _professorService;
    private readonly IPeriodoService _periodoService;
    private readonly INotaService _notaService;

    public BoletimModel(
        IAlunoService alunoService,
        IDisciplinaService disciplinaService,
        IProfessorService professorService,
        IPeriodoService periodoService,
        INotaService notaService)
    {
        _alunoService = alunoService;
        _disciplinaService = disciplinaService;
        _professorService = professorService;
        _periodoService = periodoService;
        _notaService = notaService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid AlunoId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? PeriodoId { get; set; }

    public AlunoListItemDto? Aluno { get; private set; }
    public IReadOnlyList<PeriodoTabVm> Tabs { get; private set; } = Array.Empty<PeriodoTabVm>();
    public Guid PeriodoSelecionadoId { get; private set; }
    public bool PeriodoSelecionadoAberto { get; private set; }
    public bool PodeEditar { get; private set; }
    public IReadOnlyList<DisciplinaCardVm> Disciplinas { get; private set; } = Array.Empty<DisciplinaCardVm>();
    public IReadOnlyList<ArcoVm> Arcos { get; private set; } = Array.Empty<ArcoVm>();
    public int EmDiaCount { get; private set; }
    public int AtencaoCount { get; private set; }
    public int NaoIniciadoCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para acessar o boletim de notas.";
            return RedirectToPage("/Notas/Index");
        }

        var aluno = await _alunoService.ObterPorIdAsync(AlunoId, cancellationToken);
        if (aluno is null)
        {
            TempData["ErrorMessage"] = "Aluno não encontrado.";
            return RedirectToPage("/Notas/Index");
        }

        var escopo = IsProfessorOnly()
            ? await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken)
            : null;

        if (escopo is not null && (!aluno.TurmaId.HasValue || !escopo.TurmaIds.Contains(aluno.TurmaId.Value)))
        {
            TempData["ErrorMessage"] = "Você não está vinculado à turma deste aluno.";
            return RedirectToPage("/Notas/Index");
        }

        Aluno = aluno;

        var periodosDoAno = (await _periodoService.ListarAsync(null, cancellationToken))
            .Where(x => x.AnoLetivo == aluno.AnoLetivo)
            .OrderBy(x => x.Trimestre)
            .ToList();

        if (periodosDoAno.Count == 0)
        {
            TempData["ErrorMessage"] = "Não há períodos de lançamento cadastrados para o ano letivo deste aluno.";
            return RedirectToPage("/Notas/Index");
        }

        var hoje = DateTime.UtcNow;
        var periodoSelecionado = PeriodoId.HasValue
            ? periodosDoAno.FirstOrDefault(x => x.Id == PeriodoId.Value)
            : null;
        periodoSelecionado ??= periodosDoAno.FirstOrDefault(x => PeriodoDisponibilidade.EstaAberto(x, hoje)) ?? periodosDoAno[^1];

        PeriodoSelecionadoId = periodoSelecionado.Id;
        PeriodoSelecionadoAberto = PeriodoDisponibilidade.EstaAberto(periodoSelecionado, hoje);
        PodeEditar = PeriodoSelecionadoAberto || User.IsInRole("Diretor");

        Tabs = periodosDoAno
            .Select(x => new PeriodoTabVm(x.Id, x.Trimestre, $"{x.Trimestre}º Trimestre", x.Id == periodoSelecionado.Id))
            .ToList();

        var todasDisciplinas = (await _disciplinaService.ListarAsync(null, cancellationToken))
            .Where(x => x.IsAtiva && x.Series.Any(s => s.SerieId == aluno.SerieId))
            .ToList();

        if (escopo is not null)
        {
            todasDisciplinas = todasDisciplinas.Where(x => escopo.DisciplinaIds.Contains(x.Id)).ToList();
        }

        var todosProfessores = await _professorService.ListarAsync(null, cancellationToken);
        var professorPorTurmaDisciplina = todosProfessores
            .Where(p => p.IsAtivo)
            .SelectMany(p => p.Atribuicoes.Select(a => new { p.Id, p.NomeCompleto, a.TurmaId, a.DisciplinaId }))
            .ToDictionary(x => (x.TurmaId, x.DisciplinaId), x => (x.Id, x.NomeCompleto));

        var notasDoAluno = (await _notaService.ListarAsync(null, cancellationToken))
            .Where(x => x.AlunoId == AlunoId)
            .ToList();

        var cards = new List<DisciplinaCardVm>();
        foreach (var disciplina in todasDisciplinas.OrderBy(x => x.Nome))
        {
            (Guid Id, string Nome)? professor = null;
            if (aluno.TurmaId.HasValue && professorPorTurmaDisciplina.TryGetValue((aluno.TurmaId.Value, disciplina.Id), out var encontrado))
            {
                professor = (encontrado.Id, encontrado.NomeCompleto);
            }

            var barras = periodosDoAno
                .Select(periodo =>
                {
                    var nota = notasDoAluno.FirstOrDefault(n => n.DisciplinaId == disciplina.Id && n.PeriodoLancamentoId == periodo.Id);
                    var calculo = Calcular(nota);
                    var altura = calculo.Lancado ? Math.Max(6, (int)Math.Round(Math.Min(calculo.Final, 10m) / 10m * 36m)) : 4;
                    var cor = !calculo.Lancado ? "#d8dae6" : (calculo.Final >= 5m ? "#1fb980" : "#ef4ea8");
                    return new BarraTrimestreVm(periodo.Trimestre, altura, cor, periodo.Id == periodoSelecionado.Id);
                })
                .ToList();

            var notaSelecionada = notasDoAluno.FirstOrDefault(n => n.DisciplinaId == disciplina.Id && n.PeriodoLancamentoId == periodoSelecionado.Id);
            var calc = Calcular(notaSelecionada);

            cards.Add(new DisciplinaCardVm(
                disciplina.Id,
                disciplina.Nome,
                professor?.Id,
                professor?.Nome ?? "Sem professor atribuído",
                professor.HasValue,
                notaSelecionada?.Id,
                FormatarEntrada(notaSelecionada?.Avaliacao1),
                FormatarEntrada(notaSelecionada?.Avaliacao2),
                FormatarEntrada(notaSelecionada?.Avaliacao3),
                FormatarEntrada(notaSelecionada?.RecuperacaoParalela),
                FormatarExibicao(calc.Soma),
                FormatarExibicao(calc.Final),
                calc.PodeRecuperar,
                calc.StatusLabel,
                calc.StatusClass,
                barras,
                notaSelecionada?.IsFinalizada ?? false));
        }

        Disciplinas = cards;

        var categorias = cards.Select(card =>
        {
            var mediasAnuais = periodosDoAno
                .Select(periodo => notasDoAluno.FirstOrDefault(n => n.DisciplinaId == card.DisciplinaId && n.PeriodoLancamentoId == periodo.Id))
                .Select(Calcular)
                .Where(x => x.Lancado)
                .ToList();

            if (mediasAnuais.Count == 0)
            {
                return "naoIniciado";
            }

            var mediaParcial = mediasAnuais.Average(x => x.Final);
            return mediaParcial >= 5m ? "emDia" : "atencao";
        }).ToList();

        EmDiaCount = categorias.Count(x => x == "emDia");
        AtencaoCount = categorias.Count(x => x == "atencao");
        NaoIniciadoCount = categorias.Count(x => x == "naoIniciado");

        Arcos = ConstruirArcos(categorias);

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
        var calc = Calcular(notaAtualizada);

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

    public async Task<IActionResult> OnPostFinalizarAsync(Guid id, Guid alunoId, Guid periodoId, CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar o status de notas.";
            return RedirectToPage("/Notas/Boletim", new { AlunoId = alunoId, PeriodoId = periodoId });
        }

        await _notaService.AlternarFinalizacaoAsync(id, cancellationToken);
        return RedirectToPage("/Notas/Boletim", new { AlunoId = alunoId, PeriodoId = periodoId });
    }

    public async Task<IActionResult> OnPostExcluirAsync(Guid id, Guid alunoId, Guid periodoId, CancellationToken cancellationToken)
    {
        if (!CanManageNotas())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir notas.";
            return RedirectToPage("/Notas/Boletim", new { AlunoId = alunoId, PeriodoId = periodoId });
        }

        var excluido = await _notaService.ExcluirAsync(id, cancellationToken);
        TempData[excluido ? "SuccessMessage" : "ErrorMessage"] = excluido
            ? "Lançamento excluído com sucesso."
            : "Não foi possível excluir o lançamento (o período pode estar fechado).";

        return RedirectToPage("/Notas/Boletim", new { AlunoId = alunoId, PeriodoId = periodoId });
    }

    private bool CanManageNotas() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria") || User.IsInRole("Professor");

    private bool IsProfessorOnly() =>
        User.IsInRole("Professor")
        && !User.IsInRole("Diretor")
        && !User.IsInRole("Coordenador")
        && !User.IsInRole("Cordenador")
        && !User.IsInRole("Secretaria");

    private static (bool Lancado, decimal? Soma, decimal Final, bool PodeRecuperar, string StatusLabel, string StatusClass) Calcular(NotaListItemDto? nota)
    {
        if (nota is null)
        {
            return (false, null, 0m, false, "Não lançado", "status-pill status-neutral");
        }

        var avals = new[] { nota.Avaliacao1, nota.Avaliacao2, nota.Avaliacao3 }.Where(x => x.HasValue).ToList();
        var lancado = avals.Count > 0;
        var soma = lancado ? nota.ResultadoUnidade : (decimal?)null;
        var podeRecuperar = soma.HasValue && soma.Value < 5m;
        var final = nota.ResultadoFinalUnidade;

        string statusLabel;
        string statusClass;
        if (!lancado)
        {
            statusLabel = "Não lançado";
            statusClass = "status-pill status-neutral";
        }
        else if (avals.Count < 3)
        {
            statusLabel = "Em andamento";
            statusClass = "status-pill status-warn";
        }
        else
        {
            statusLabel = final >= 5m ? "Aprovado" : "Reprovado";
            statusClass = final >= 5m ? "status-pill status-open" : "status-pill status-danger";
        }

        return (lancado, soma, final, podeRecuperar, statusLabel, statusClass);
    }

    private static IReadOnlyList<ArcoVm> ConstruirArcos(IReadOnlyList<string> categorias)
    {
        if (categorias.Count == 0)
        {
            return Array.Empty<ArcoVm>();
        }

        var cores = new Dictionary<string, string> { ["emDia"] = "#1fb980", ["atencao"] = "#ef4ea8", ["naoIniciado"] = "#d8dae6" };
        const double raio = 42;
        var circunferencia = 2 * Math.PI * raio;
        var segmento = circunferencia / categorias.Count;
        const double vao = 6;

        return categorias
            .Select((categoria, indice) => new ArcoVm(
                cores[categoria],
                $"{Math.Max(segmento - vao, 4).ToString("0.0", CultureInfo.InvariantCulture)} {circunferencia.ToString("0.0", CultureInfo.InvariantCulture)}",
                (-(indice * segmento)).ToString("0.0", CultureInfo.InvariantCulture)))
            .ToList();
    }

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

    public sealed record PeriodoTabVm(Guid Id, int Trimestre, string Label, bool Ativo);

    public sealed record BarraTrimestreVm(int Trimestre, int AlturaPx, string Cor, bool Ativo);

    public sealed record ArcoVm(string Cor, string DashArray, string DashOffset);

    public sealed record DisciplinaCardVm(
        Guid DisciplinaId,
        string DisciplinaNome,
        Guid? ProfessorId,
        string ProfessorNome,
        bool TemProfessorAtribuido,
        Guid? NotaId,
        string Avaliacao1,
        string Avaliacao2,
        string Avaliacao3,
        string RecuperacaoParalela,
        string Soma,
        string ResultadoFinal,
        bool PodeRecuperar,
        string StatusLabel,
        string StatusClass,
        IReadOnlyList<BarraTrimestreVm> Barras,
        bool Finalizada);
}
