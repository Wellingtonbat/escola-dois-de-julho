using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Web.Pages.Professores;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly IProfessorService _professorService;
    private readonly ISerieService _serieService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly ITurmaService _turmaService;

    public IndexModel(
        IProfessorService professorService,
        ISerieService serieService,
        IDisciplinaService disciplinaService,
        ITurmaService turmaService)
    {
        _professorService = professorService;
        _serieService = serieService;
        _disciplinaService = disciplinaService;
        _turmaService = turmaService;
    }

    public IReadOnlyList<ProfessorListItemDto> Professores { get; private set; } = Array.Empty<ProfessorListItemDto>();
    public IReadOnlyList<SelectListItem> Series { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Disciplinas { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<TurmaListItemDto> TurmasAtivas { get; private set; } = Array.Empty<TurmaListItemDto>();

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = PageSizeFixo;

    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (IsProfessorOnly())
        {
            TempData["ErrorMessage"] = "Seu perfil não tem acesso a esta tela.";
            return RedirectToPage("/Notas/Index");
        }

        await LoadSeriesAsync(cancellationToken);
        await LoadDisciplinasAsync(cancellationToken);
        await LoadTurmasAsync(cancellationToken);

        bool? statusFilter = Status switch
        {
            "ativos" => true,
            "inativos" => false,
            _ => null
        };

        var filter = new ProfessorListFilter(Busca, statusFilter);
        var professoresFiltrados = await _professorService.ListarAsync(filter, cancellationToken);

        TotalCount = professoresFiltrados.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Professores = professoresFiltrados
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string nomeCompleto,
        string email,
        string usuarioCpf,
        List<AtribuicaoInputModel> atribuicoes,
        string senha,
        bool isAtivo,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageProfessores())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar professores.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _professorService.CriarAsync(
            new ProfessorCreateRequest(
                nomeCompleto,
                email,
                usuarioCpf,
                MapearAtribuicoes(atribuicoes),
                isAtivo,
                senha),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível cadastrar o professor.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Professor cadastrado com sucesso. Ele deverá trocar a senha padrão no primeiro acesso.";
        return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        string nomeCompleto,
        string email,
        string usuarioCpf,
        List<AtribuicaoInputModel> atribuicoes,
        bool isAtivo,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageProfessores())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar professores.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _professorService.AtualizarAsync(
            id,
            new ProfessorCreateRequest(nomeCompleto, email, usuarioCpf, MapearAtribuicoes(atribuicoes), isAtivo),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível atualizar o professor.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Professor atualizado com sucesso.";
        return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(
        Guid id,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageProfessores())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar status de professores.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var updated = await _professorService.AlternarStatusAsync(id, cancellationToken);
        if (!updated)
        {
            TempData["ErrorMessage"] = "Professor não encontrado para atualização de status.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Status do professor atualizado com sucesso.";
        return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid id,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageProfessores())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir professores.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var deleted = await _professorService.ExcluirAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Professor não encontrado para exclusão.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Professor excluído com sucesso.";
        return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostRedefinirSenhaAsync(
        Guid id,
        string novaSenha,
        string? busca,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageProfessores())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para redefinir a senha de professores.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _professorService.RedefinirSenhaAsync(id, novaSenha, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível redefinir a senha do professor.";
            return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Senha redefinida com sucesso. O professor deverá trocá-la no próximo acesso.";
        return RedirectToPage("/Professores/Index", new { Busca = busca, Status = status, PageNumber = pageNumber, PageSize = pageSize });
    }

    private bool CanManageProfessores() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    private bool IsProfessorOnly() => User.IsInRole("Professor") && !CanManageProfessores();

    private static List<ProfessorAtribuicaoInput> MapearAtribuicoes(List<AtribuicaoInputModel>? atribuicoes) =>
        (atribuicoes ?? new List<AtribuicaoInputModel>())
            .Where(x => x.TurmaId != Guid.Empty && x.DisciplinaId != Guid.Empty)
            .Select(x => new ProfessorAtribuicaoInput(x.TurmaId, x.DisciplinaId))
            .ToList();

    private async Task LoadSeriesAsync(CancellationToken cancellationToken)
    {
        var series = await _serieService.ListarAsync(new SerieListFilter(null, true), cancellationToken);
        Series = series
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.Nome))
            .ToList();
    }

    private async Task LoadDisciplinasAsync(CancellationToken cancellationToken)
    {
        var disciplinas = await _disciplinaService.ListarAsync(new DisciplinaListFilter(null, true), cancellationToken);
        Disciplinas = disciplinas
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.Id.ToString()))
            .ToList();
    }

    private async Task LoadTurmasAsync(CancellationToken cancellationToken)
    {
        var turmas = await _turmaService.ListarAsync(new TurmaListFilter(null, null, null, true), cancellationToken);
        TurmasAtivas = turmas
            .OrderBy(x => x.SerieNome)
            .ThenBy(x => x.Nome)
            .ToList();
    }

    public sealed class AtribuicaoInputModel
    {
        public Guid TurmaId { get; set; }
        public Guid DisciplinaId { get; set; }
    }
}
