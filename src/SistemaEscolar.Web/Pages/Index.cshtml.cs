using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Dashboard;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IProfessorService _professorService;
    private readonly ITurmaService _turmaService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IPeriodoService _periodoService;
    private readonly IDashboardService _dashboardService;

    public IndexModel(
        IProfessorService professorService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService,
        IPeriodoService periodoService,
        IDashboardService dashboardService)
    {
        _professorService = professorService;
        _turmaService = turmaService;
        _disciplinaService = disciplinaService;
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

    // Professor sem perfil de gestão: no lugar do select de Professor, vê o campo "Visão" com
    // "Minhas turmas" (o próprio ProfessorId, padrão) e "Escola inteira" (sem professor).
    public bool EhApenasProfessor { get; private set; }

    public Guid? ProfessorLogadoId { get; private set; }

    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = Array.Empty<int>();
    public IReadOnlyList<SelectListItem> Professores { get; private set; } = Array.Empty<SelectListItem>();
    // Turmas de todos os anos: a tela mostra só as do ano letivo selecionado (atributo data-ano + dashboard.js),
    // para que trocar o ano no filtro não exija recarregar a página.
    public IReadOnlyList<TurmaListItemDto> Turmas { get; private set; } = Array.Empty<TurmaListItemDto>();
    public IReadOnlyList<SelectListItem> Disciplinas { get; private set; } = Array.Empty<SelectListItem>();
    public DashboardDadosDto? Dados { get; private set; }
    public IReadOnlyList<PeriodoListItemDto> PeriodosAbertosComDataVencida { get; private set; } = Array.Empty<PeriodoListItemDto>();

    public string DadosIniciaisJson => Dados is null
        ? "null"
        : JsonSerializer.Serialize(Dados, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        EhApenasProfessor = User.EhApenasProfessor();

        var periodos = await _periodoService.ListarAsync(null, cancellationToken);
        if (!EhApenasProfessor)
        {
            var hoje = HorarioBrasilia.Agora;
            PeriodosAbertosComDataVencida = periodos
                .Where(p => PeriodoDisponibilidade.EstaForaDaJanelaPorExcecaoManual(p, hoje))
                .OrderBy(p => p.AnoLetivo).ThenBy(p => p.Trimestre)
                .ToList();
        }

        AnosDisponiveis = periodos.Select(p => p.AnoLetivo).Distinct().OrderByDescending(x => x).ToList();
        var anoLetivo = AnoLetivo ?? AnosDisponiveis.FirstOrDefault(HorarioBrasilia.Agora.Year);

        if (EhApenasProfessor)
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken);
            ProfessorLogadoId = escopo?.ProfessorId ?? Guid.Empty;
            // Primeira abertura: "Minhas turmas". O serviço garante que o professor só filtre por ele mesmo.
            ProfessorId ??= ProfessorLogadoId;
        }
        else
        {
            Professores = (await _professorService.ListarAsync(null, cancellationToken))
                .Where(p => p.IsAtivo)
                .OrderBy(p => p.NomeCompleto)
                .Select(p => new SelectListItem(p.NomeCompleto, p.Id.ToString()))
                .ToList();
        }

        Turmas = (await _turmaService.ListarAsync(new TurmaListFilter(null, null, null, true), cancellationToken))
            .OrderBy(t => t.Nome)
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
        if (!PodeVerDashboard())
        {
            return Forbid();
        }

        var filtro = new DashboardFiltroDto(anoLetivo, trimestre, professorId, turmaId, disciplinaId);
        var dados = await _dashboardService.ObterDadosAsync(filtro, cancellationToken);
        return new JsonResult(dados);
    }

    private bool PodeVerDashboard() =>
        User.EhGestao() || User.IsInRole(Perfis.Professor);
}
