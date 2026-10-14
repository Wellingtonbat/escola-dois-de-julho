using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Web.Extensions;

namespace SistemaEscolar.Web.Pages.Periodos;

public sealed class IndexModel : PageModel
{
    private readonly IPeriodoService _periodoService;
    private readonly IPendenciasLancamentoService _pendenciasService;

    public IndexModel(IPeriodoService periodoService, IPendenciasLancamentoService pendenciasService)
    {
        _periodoService = periodoService;
        _pendenciasService = pendenciasService;
    }

    public IReadOnlyList<PeriodoListItemDto> Periodos { get; private set; } = Array.Empty<PeriodoListItemDto>();

    [BindProperty(SupportsGet = true)]
    public int? AnoLetivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Trimestre { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "todos";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (IsProfessorOnly())
        {
            TempData["ErrorMessage"] = "Seu perfil não tem acesso a esta tela.";
            return RedirectToPage("/Notas/Index");
        }

        bool? statusFilter = Status switch
        {
            "abertos" => true,
            "fechados" => false,
            _ => null
        };

        var filter = new PeriodoListFilter(AnoLetivo, Trimestre, statusFilter);
        Periodos = await _periodoService.ListarAsync(filter, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        int anoLetivo,
        int trimestre,
        string descricao,
        DateTime dataInicial,
        DateTime dataFinal,
        CancellationToken cancellationToken)
    {
        if (!CanManagePeriodos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para cadastrar períodos.";
            return RedirectToPage();
        }

        var result = await _periodoService.CriarAsync(
            new PeriodoCreateRequest(anoLetivo, trimestre, descricao, dataInicial, dataFinal),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período cadastrado com sucesso."
            : result.ErrorMessage ?? "Não foi possível cadastrar o período.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid id,
        int anoLetivo,
        int trimestre,
        string descricao,
        DateTime dataInicial,
        DateTime dataFinal,
        CancellationToken cancellationToken)
    {
        if (!CanManagePeriodos())
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar períodos.";
            return RedirectToPage();
        }

        var result = await _periodoService.AtualizarAsync(
            id,
            new PeriodoCreateRequest(anoLetivo, trimestre, descricao, dataInicial, dataFinal),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período atualizado com sucesso."
            : result.ErrorMessage ?? "Não foi possível atualizar o período.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAbrirAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!CanAbrirFecharPeriodo())
        {
            TempData["ErrorMessage"] = "Somente Diretor, Vice-Diretor ou Coordenador podem abrir períodos.";
            return RedirectToPage();
        }

        var result = await _periodoService.AbrirAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período aberto com sucesso."
            : result.ErrorMessage ?? "Não foi possível abrir o período.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFecharAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!CanAbrirFecharPeriodo())
        {
            TempData["ErrorMessage"] = "Somente Diretor, Vice-Diretor ou Coordenador podem fechar períodos.";
            return RedirectToPage();
        }

        var result = await _periodoService.FecharAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Período fechado com sucesso."
            : result.ErrorMessage ?? "Não foi possível fechar o período.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!User.PodeExcluirPeriodo())
        {
            TempData["ErrorMessage"] = "Somente Diretor ou Vice-Diretor podem excluir períodos.";
            return RedirectToPage();
        }

        var deleted = await _periodoService.ExcluirAsync(id, cancellationToken);
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
            ? "Período excluído com sucesso."
            : "Período não encontrado para exclusão.";

        return RedirectToPage();
    }

    // Chamado pela tela antes de fechar: se houver pendências, ela mostra a lista e pergunta se deseja
    // baixá-la e se deseja fechar mesmo assim.
    public async Task<IActionResult> OnGetPendenciasAsync(Guid id, CancellationToken cancellationToken)
    {
        var pendencias = await _pendenciasService.ListarAsync(id, cancellationToken);
        return pendencias is null ? Forbid() : new JsonResult(pendencias);
    }

    public async Task<IActionResult> OnGetPendenciasPdfAsync(Guid id, CancellationToken cancellationToken)
    {
        var pendencias = await _pendenciasService.ListarAsync(id, cancellationToken);
        if (pendencias is null)
        {
            TempData["ErrorMessage"] = "Período não encontrado ou sem permissão para ver as pendências.";
            return RedirectToPage();
        }

        var nomeArquivo = $"pendencias-{pendencias.PeriodoNome}.pdf".Replace(" ", "-").Replace("/", "-").ToLowerInvariant();
        return File(GerarPdfPendencias(pendencias), "application/pdf", nomeArquivo);
    }

    private static byte[] GerarPdfPendencias(PendenciasPeriodoDto pendencias)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Escola Dois de Julho").SemiBold().FontSize(13).FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Lançamentos pendentes — {pendencias.PeriodoNome}").Bold().FontSize(11);
                    col.Item().PaddingBottom(8).Text(
                        "Sem nota: aluno sem nenhuma nota no trimestre. Incompleta: falta alguma das 3 avaliações. " +
                        "Não finalizada: nota lançada que ainda não foi finalizada.").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(col =>
                {
                    if (!pendencias.TemPendencias)
                    {
                        col.Item().Text("Nenhum lançamento pendente neste período.");
                        return;
                    }

                    foreach (var professor in pendencias.Itens.GroupBy(i => i.ProfessorNome))
                    {
                        col.Item().PaddingTop(8).PaddingBottom(3).Text(professor.Key).Bold().FontSize(10);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2.2f);
                                c.RelativeColumn(1.6f);
                                c.RelativeColumn(0.9f);
                                c.RelativeColumn(0.9f);
                                c.RelativeColumn(0.9f);
                                c.RelativeColumn(1.1f);
                            });

                            table.Header(h =>
                            {
                                foreach (var titulo in new[] { "Disciplina", "Turma", "Sem nota", "Incompleta", "Não finalizada", "Alunos na turma" })
                                {
                                    h.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                                        .Padding(3).Text(titulo).Bold().FontSize(8);
                                }
                            });

                            foreach (var item in professor)
                            {
                                foreach (var valor in new[]
                                {
                                    item.DisciplinaNome, item.TurmaNome,
                                    item.SemNota.ToString(ptBr), item.Incompletas.ToString(ptBr),
                                    item.NaoFinalizadas.ToString(ptBr), item.TotalAlunos.ToString(ptBr)
                                })
                                {
                                    table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor);
                                }
                            }
                        });
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Emitido em {HorarioBrasilia.Agora:dd/MM/yyyy HH:mm}").FontSize(8);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.DefaultTextStyle(t => t.FontSize(8));
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private bool CanManagePeriodos() => User.EhGestao();

    private bool CanAbrirFecharPeriodo() => User.PodeAbrirFecharPeriodo();

    private bool IsProfessorOnly() => User.EhApenasProfessor();
}
