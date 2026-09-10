using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Resultados;

namespace SistemaEscolar.Web.Pages.Notas;

public sealed class BoletimModel : PageModel
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IAlunoService _alunoService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IProfessorService _professorService;
    private readonly IPeriodoService _periodoService;
    private readonly INotaService _notaService;
    private readonly IRecuperacaoFinalService _recuperacaoFinalService;

    public BoletimModel(
        IAlunoService alunoService,
        IDisciplinaService disciplinaService,
        IProfessorService professorService,
        IPeriodoService periodoService,
        INotaService notaService,
        IRecuperacaoFinalService recuperacaoFinalService)
    {
        _alunoService = alunoService;
        _disciplinaService = disciplinaService;
        _professorService = professorService;
        _periodoService = periodoService;
        _notaService = notaService;
        _recuperacaoFinalService = recuperacaoFinalService;
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

    public async Task<IActionResult> OnGetBaixarPdfAsync(CancellationToken cancellationToken)
    {
        if (!PodeGerarBoletimPdf())
        {
            TempData["ErrorMessage"] = "Somente Diretor, Coordenador ou Secretaria podem baixar o boletim em PDF.";
            return RedirectToPage("/Notas/Boletim", new { AlunoId, PeriodoId });
        }

        var aluno = await _alunoService.ObterPorIdAsync(AlunoId, cancellationToken);
        if (aluno is null)
        {
            TempData["ErrorMessage"] = "Aluno não encontrado.";
            return RedirectToPage("/Notas/Index");
        }

        var periodosDoAno = (await _periodoService.ListarAsync(null, cancellationToken))
            .Where(x => x.AnoLetivo == aluno.AnoLetivo)
            .OrderBy(x => x.Trimestre)
            .ToList();

        var todasDisciplinas = (await _disciplinaService.ListarAsync(null, cancellationToken))
            .Where(x => x.IsAtiva && x.Series.Any(s => s.SerieId == aluno.SerieId))
            .OrderBy(x => x.Nome)
            .ToList();

        var notasDoAluno = (await _notaService.ListarAsync(null, cancellationToken))
            .Where(x => x.AlunoId == AlunoId)
            .ToList();

        var recuperacoesFinais = await _recuperacaoFinalService.ListarPorAlunoEAnoAsync(AlunoId, aluno.AnoLetivo, cancellationToken);

        var numeroChamada = 1;
        if (aluno.TurmaId.HasValue)
        {
            var colegas = (await _alunoService.ListarAsync(new AlunoListFilter(null, null, aluno.TurmaId, true), cancellationToken))
                .OrderBy(x => x.NomeCompleto, StringComparer.Create(PtBr, ignoreCase: true))
                .ToList();
            var indice = colegas.FindIndex(x => x.Id == AlunoId);
            numeroChamada = indice >= 0 ? indice + 1 : 1;
        }

        var linhas = todasDisciplinas
            .Select(disciplina => ConstruirLinhaBoletim(disciplina, periodosDoAno, notasDoAluno, recuperacoesFinais))
            .ToList();

        var pdfBytes = GerarPdfBoletim(aluno, numeroChamada, linhas);
        var nomeArquivo = $"boletim-{aluno.NomeCompleto}-{aluno.AnoLetivo}.pdf".Replace(' ', '-');
        return File(pdfBytes, "application/pdf", nomeArquivo);
    }

    private static BoletimLinhaVm ConstruirLinhaBoletim(
        DisciplinaListItemDto disciplina,
        IReadOnlyList<PeriodoListItemDto> periodosDoAno,
        IReadOnlyList<NotaListItemDto> notasDoAluno,
        IReadOnlyDictionary<Guid, decimal> recuperacoesFinais)
    {
        var notasDaDisciplina = notasDoAluno.Where(n => n.DisciplinaId == disciplina.Id).ToList();
        recuperacoesFinais.TryGetValue(disciplina.Id, out var recuperacaoValor);
        var temRecuperacaoFinal = recuperacoesFinais.ContainsKey(disciplina.Id);

        var resultado = BoletimCalculo.Calcular(periodosDoAno, notasDaDisciplina, temRecuperacaoFinal ? recuperacaoValor : null);

        return new BoletimLinhaVm(
            disciplina.Nome,
            resultado.Trimestres.Select(t => new BoletimTrimestreVm(t.Av1, t.Av2, t.Av3, t.ResUnidade, t.RecParalela, t.ResFinal)).ToList(),
            resultado.TemLancamento,
            BoletimCalculo.FormatarNumero(resultado.TotalPontos),
            resultado.MediaCurso.HasValue ? BoletimCalculo.FormatarNumero(resultado.MediaCurso.Value) : "—",
            resultado.RecuperacaoFinal.HasValue ? BoletimCalculo.FormatarNumero(resultado.RecuperacaoFinal.Value) : "—",
            resultado.Situacao);
    }

    private static byte[] GerarPdfBoletim(AlunoListItemDto aluno, int numeroChamada, IReadOnlyList<BoletimLinhaVm> linhas)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var aprovadas = linhas.Count(x => x.Situacao == "AP");
        var reprovadas = linhas.Count(x => x.Situacao == "RP");
        var semResultado = linhas.Count - aprovadas - reprovadas;
        const int colunasPorTrimestre = 6;
        const int totalTrimestres = 3;
        const int colunasResumo = 4;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(esquerda =>
                        {
                            esquerda.Item().Text("Escola Municipal 2 de Julho").Bold().FontSize(13);
                            esquerda.Item().Text("Secretaria Municipal de Educação - Salvador, Bahia");
                            esquerda.Item().Text($"Ano Letivo {aluno.AnoLetivo} - Sistema de Gestão Acadêmica");
                        });

                        row.RelativeItem().Column(direita =>
                        {
                            direita.Item().AlignRight().Text(aluno.NomeCompleto.ToUpperInvariant()).Bold().FontSize(12);
                            direita.Item().AlignRight().Row(pills =>
                            {
                                pills.Spacing(4);
                                pills.AutoItem().Element(PillStyle).Text(aluno.Serie);
                                pills.AutoItem().Element(PillStyle).Text($"Turma {aluno.TurmaNome ?? "-"}");
                                pills.AutoItem().Element(PillStyle).Text($"Nº {numeroChamada}");
                            });
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(conteudo =>
                {
                conteudo.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3.4f);
                        for (var t = 0; t < totalTrimestres; t++)
                        {
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        }

                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(0.8f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().RowSpan(2).Element(CabecalhoPrincipal).AlignMiddle().Text("Disciplina");
                        header.Cell().ColumnSpan(colunasPorTrimestre).Element(CabecalhoPrincipal).AlignCenter().Text("1º Trimestre");
                        header.Cell().ColumnSpan(colunasPorTrimestre).Element(CabecalhoPrincipal).AlignCenter().Text("2º Trimestre");
                        header.Cell().ColumnSpan(colunasPorTrimestre).Element(CabecalhoPrincipal).AlignCenter().Text("3º Trimestre");
                        header.Cell().RowSpan(2).Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("Tot.Pts");
                        header.Cell().RowSpan(2).Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("Méd.Curso");
                        header.Cell().RowSpan(2).Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("Rec.");
                        header.Cell().RowSpan(2).Element(CabecalhoPrincipal).AlignCenter().AlignMiddle().Text("Res.");

                        for (var t = 0; t < totalTrimestres; t++)
                        {
                            header.Cell().Element(CabecalhoSecundario).AlignCenter().Text("Av1");
                            header.Cell().Element(CabecalhoSecundario).AlignCenter().Text("Av2");
                            header.Cell().Element(CabecalhoSecundario).AlignCenter().Text("Av3");
                            header.Cell().Element(CabecalhoSecundario).AlignCenter().Text("Res.Un");
                            header.Cell().Element(CabecalhoSecundario).AlignCenter().Text("Rec.Par");
                            header.Cell().Element(CabecalhoSecundario).AlignCenter().Text("Res.Fi");
                        }
                    });

                    foreach (var linha in linhas)
                    {
                        table.Cell().Element(CelulaDisciplina).AlignMiddle().Text(linha.Disciplina).Bold();

                        if (!linha.TemLancamento)
                        {
                            table.Cell().ColumnSpan(colunasPorTrimestre * totalTrimestres + colunasResumo).Element(CelulaSemDados).AlignCenter().AlignMiddle().Text("Sem dados lançados").Italic();
                            continue;
                        }

                        foreach (var trimestre in linha.Trimestres)
                        {
                            table.Cell().Element(Celula).AlignCenter().Text(trimestre.Av1);
                            table.Cell().Element(Celula).AlignCenter().Text(trimestre.Av2);
                            table.Cell().Element(Celula).AlignCenter().Text(trimestre.Av3);
                            table.Cell().Element(Celula).AlignCenter().Text(trimestre.ResUnidade).Bold();
                            table.Cell().Element(Celula).AlignCenter().Text(trimestre.RecParalela);
                            table.Cell().Element(Celula).AlignCenter().Text(trimestre.ResFinal).Bold();
                        }

                        table.Cell().Element(Celula).AlignCenter().Text(linha.TotalPontos).Bold();
                        table.Cell().Element(Celula).AlignCenter().Text(linha.MediaCurso).Bold();
                        table.Cell().Element(Celula).AlignCenter().Text(linha.RecuperacaoFinal);
                        table.Cell().Element(CelulaSituacao(linha.Situacao)).AlignCenter().Text(linha.Situacao).Bold();
                    }
                });

                conteudo.Item().PaddingTop(8).Text($"{aprovadas} aprovada(s)   |   {reprovadas} reprovada(s)   |   {semResultado} sem resultado");

                if (reprovadas > 0)
                {
                    conteudo.Item().PaddingTop(4).Background(Colors.Red.Lighten4).Padding(4)
                        .Text($"Com reprovação em {reprovadas} disciplina(s)").Bold().FontColor(Colors.Red.Darken2);
                }

                // Espaço fixo e generoso até o bloco de assinaturas — um espaçador flexível
                // (ExtendVertical) aqui empurra a linha inteira para uma segunda página em
                // branco, já que ele reserva 100% do espaço restante sem sobrar nada para o
                // conteúdo seguinte.
                conteudo.Item().PaddingTop(120);

                conteudo.Item().PaddingBottom(10).Row(row =>
                {
                    row.Spacing(20);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Height(40);
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                        c.Item().AlignCenter().PaddingTop(3).Text("Direção");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Height(40);
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                        c.Item().AlignCenter().PaddingTop(3).Text("Coordenação Pedagógica");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Height(40);
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                        c.Item().AlignCenter().PaddingTop(3).Text("Responsável pelo Aluno");
                    });
                });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer PillStyle(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).PaddingVertical(2).PaddingHorizontal(6).DefaultTextStyle(x => x.FontSize(8).SemiBold());

    private static IContainer CabecalhoPrincipal(IContainer container) =>
        container.Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).DefaultTextStyle(x => x.Bold().FontSize(7.5f));

    private static IContainer CabecalhoSecundario(IContainer container) =>
        container.Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(2).DefaultTextStyle(x => x.Bold().FontSize(6.5f));

    private static IContainer Celula(IContainer container) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).DefaultTextStyle(x => x.FontSize(7));

    private static IContainer CelulaDisciplina(IContainer container) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).DefaultTextStyle(x => x.FontSize(7.5f));

    private static IContainer CelulaSemDados(IContainer container) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken1));

    private static Func<IContainer, IContainer> CelulaSituacao(string situacao) => container =>
    {
        var corFundo = situacao switch
        {
            "AP" => Colors.Green.Lighten4,
            "RP" => Colors.Red.Lighten4,
            _ => Colors.White
        };
        var corTexto = situacao switch
        {
            "AP" => Colors.Green.Darken2,
            "RP" => Colors.Red.Darken2,
            _ => Colors.Black
        };

        return container.Background(corFundo).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).DefaultTextStyle(x => x.FontSize(7).FontColor(corTexto));
    };

    private bool PodeGerarBoletimPdf() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria");

    public sealed record BoletimTrimestreVm(string Av1, string Av2, string Av3, string ResUnidade, string RecParalela, string ResFinal);

    public sealed record BoletimLinhaVm(
        string Disciplina,
        IReadOnlyList<BoletimTrimestreVm> Trimestres,
        bool TemLancamento,
        string TotalPontos,
        string MediaCurso,
        string RecuperacaoFinal,
        string Situacao);

    private bool CanManageNotas() =>
        User.IsInRole("Diretor") || User.IsInRole("Coordenador") || User.IsInRole("Cordenador") || User.IsInRole("Secretaria") || User.IsInRole("Professor");

    private bool IsProfessorOnly() =>
        User.IsInRole("Professor")
        && !User.IsInRole("Diretor")
        && !User.IsInRole("Coordenador")
        && !User.IsInRole("Cordenador")
        && !User.IsInRole("Secretaria");

    private static NotaCalculo.Resultado Calcular(NotaListItemDto? nota) => NotaCalculo.Calcular(nota);

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
