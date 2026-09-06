using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Web.Pages.Alunos;

public sealed class IndexModel : PageModel
{
    private const int PageSizeFixo = 50;
    private readonly IAlunoService _alunoService;
    private readonly IAlunoImportService _alunoImportService;
    private readonly ISerieService _serieService;
    private readonly ITurmaService _turmaService;
    private readonly IProfessorService _professorService;

    public IndexModel(
        IAlunoService alunoService,
        IAlunoImportService alunoImportService,
        ISerieService serieService,
        ITurmaService turmaService,
        IProfessorService professorService)
    {
        _alunoService = alunoService;
        _alunoImportService = alunoImportService;
        _serieService = serieService;
        _turmaService = turmaService;
        _professorService = professorService;
    }

    public IReadOnlyList<AlunoListItemDto> Alunos { get; private set; } = Array.Empty<AlunoListItemDto>();
    public IReadOnlyList<SelectListItem> Series { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> SeriesParaFormulario { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Turmas { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<TurmaListItemDto> TurmasAtivas { get; private set; } = Array.Empty<TurmaListItemDto>();

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public string? Serie { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? TurmaId { get; set; }

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
        await LoadTurmasAsync(cancellationToken);

        bool? statusFilter = Status switch
        {
            "ativos" => true,
            "inativos" => false,
            _ => null
        };

        var filter = new AlunoListFilter(Busca, Serie, TurmaId, statusFilter);
        var alunosFiltrados = await _alunoService.ListarAsync(filter, cancellationToken);

        if (IsProfessorOnly())
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(User.Identity?.Name, cancellationToken);
            if (escopo is not null)
            {
                alunosFiltrados = alunosFiltrados
                    .Where(x => x.TurmaId.HasValue && escopo.TurmaIds.Contains(x.TurmaId.Value))
                    .ToList();
            }
        }

        TotalCount = alunosFiltrados.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Alunos = alunosFiltrados
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string cpf,
        string nomeCompleto,
        DateTime dataNascimento,
        int anoLetivo,
        Guid serieId,
        Guid? turmaId,
        bool isAtivo,
        string? busca,
        string? status,
        string? serieFilter,
        Guid? turmaFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageAlunos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar alunos.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serieFilter, TurmaId = turmaFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _alunoService.CriarAsync(
            new AlunoCreateRequest(cpf, nomeCompleto, dataNascimento, anoLetivo, serieId, turmaId, isAtivo),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível cadastrar o aluno.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serieFilter, TurmaId = turmaFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Aluno cadastrado com sucesso.";
        return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serieFilter, TurmaId = turmaFilter, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        string cpf,
        string nomeCompleto,
        DateTime dataNascimento,
        int anoLetivo,
        Guid serieId,
        Guid? turmaId,
        bool isAtivo,
        string? busca,
        string? status,
        string? serieFilter,
        Guid? turmaFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageAlunos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar alunos.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serieFilter, TurmaId = turmaFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        var result = await _alunoService.AtualizarAsync(
            id,
            new AlunoCreateRequest(cpf, nomeCompleto, dataNascimento, anoLetivo, serieId, turmaId, isAtivo),
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Não foi possível atualizar o aluno.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serieFilter, TurmaId = turmaFilter, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Aluno atualizado com sucesso.";
        return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serieFilter, TurmaId = turmaFilter, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(
        Guid id,
        string? busca,
        string? status,
        string? serie,
        Guid? turma,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageAlunos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar o status de alunos.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        var updated = await _alunoService.AlternarStatusAsync(id, cancellationToken);

        if (!updated)
        {
            TempData["ErrorMessage"] = "Aluno não encontrado para atualização de status.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Status do aluno atualizado com sucesso.";
        return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid id,
        string? busca,
        string? status,
        string? serie,
        Guid? turma,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageAlunos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para excluir alunos.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        var deleted = await _alunoService.ExcluirAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Aluno não encontrado para exclusão.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = "Aluno excluído com sucesso.";
        return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnPostImportAsync(
        IFormFile? arquivoImportacao,
        Guid serieId,
        Guid? turmaId,
        int anoLetivo,
        string? busca,
        string? status,
        string? serie,
        Guid? turma,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanManageAlunos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para importar alunos.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (serieId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Selecione a série para a qual os alunos serão matriculados.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (anoLetivo < 2000 || anoLetivo > 2100)
        {
            TempData["ErrorMessage"] = "Informe um ano letivo válido para a importação.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (arquivoImportacao is null || arquivoImportacao.Length == 0)
        {
            TempData["ErrorMessage"] = "Selecione um arquivo CSV ou Excel para importar.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        var extension = Path.GetExtension(arquivoImportacao.FileName);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Formato inválido. A importação aceita arquivos CSV (.csv) ou Excel (.xlsx).";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        const long maxSize = 10 * 1024 * 1024;
        if (arquivoImportacao.Length > maxSize)
        {
            TempData["ErrorMessage"] = "Arquivo acima do limite de 10MB.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        await using var stream = arquivoImportacao.OpenReadStream();

        var result = await _alunoImportService.ImportarAsync(arquivoImportacao.FileName, stream, serieId, turmaId, anoLetivo, cancellationToken);
        if (result.Importados + result.Falhas > 5000)
        {
            TempData["ErrorMessage"] = "A importação permite no máximo 5.000 registros por arquivo.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        if (result.Importados == 0)
        {
            TempData["ErrorMessage"] = "Nenhum aluno foi importado. Verifique o layout e os dados do arquivo.";
            return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
        }

        TempData["SuccessMessage"] = result.Falhas > 0
            ? $"Importação concluída com {result.Importados} sucesso(s) e {result.Falhas} falha(s)."
            : $"Importação concluída com {result.Importados} aluno(s) incluído(s).";

        return RedirectToPage("/Alunos/Index", new { Busca = busca, Status = status, Serie = serie, TurmaId = turma, PageNumber = pageNumber, PageSize = pageSize });
    }

    public async Task<IActionResult> OnGetModeloImportacaoAsync(CancellationToken cancellationToken)
    {
        if (!CanManageAlunos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para baixar o modelo de importação.";
            return RedirectToPage("/Alunos/Index", new { Busca, Status, Serie, TurmaId, PageNumber, PageSize });
        }

        var template = await _alunoImportService.GerarModeloExcelAsync(cancellationToken);
        return File(template.Content, template.ContentType, template.FileName);
    }

    private bool CanManageAlunos() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    private bool IsProfessorOnly() => User.IsInRole("Professor") && !CanManageAlunos();

    private async Task LoadSeriesAsync(CancellationToken cancellationToken)
    {
        var series = await _serieService.ListarAsync(new SerieListFilter(null, true), cancellationToken);
        var seriesOrdenadas = series
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .ToList();

        Series = seriesOrdenadas
            .Select(x => new SelectListItem(x.Nome, x.Nome))
            .ToList();

        SeriesParaFormulario = seriesOrdenadas
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

        var turmasFiltradas = string.IsNullOrWhiteSpace(Serie)
            ? TurmasAtivas
            : TurmasAtivas.Where(x => x.SerieNome == Serie.Trim());

        Turmas = turmasFiltradas
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.Id.ToString()))
            .ToList();
    }
}
