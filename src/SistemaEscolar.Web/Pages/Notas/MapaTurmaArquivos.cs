using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaEscolar.Application.Notas;

namespace SistemaEscolar.Web.Pages.Notas;

// Arquivos do Mapa da Turma (tela Notas): a mesma informação em Excel (uma planilha só) e em PDF (paisagem,
// com as disciplinas divididas em blocos quando não cabem numa página).
public static class MapaTurmaArquivos
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] Subcolunas = { "1º T", "2º T", "3º T", "Média" };
    private const int DisciplinasPorBlocoPdf = 7;

    public const string Legenda =
        "Nota do trimestre = resultado final da unidade (maior entre a soma das avaliações e a recuperação paralela). " +
        "Média = média dos trimestres com lançamento completo. Em vermelho: abaixo da média (5,0). " +
        "\"—\" = nota não lançada em trimestre já iniciado. Em branco = trimestre ainda não iniciado. " +
        "* = lançamento incompleto (faltam avaliações); fica fora das médias até ser completado.";

    public static string NomeArquivo(MapaTurmaDto mapa, string extensao) =>
        $"mapa-turma-{mapa.TurmaNome}-{mapa.AnoLetivo}.{extensao}"
            .Replace(" ", "-").Replace("/", "-").ToLowerInvariant();

    // ---------------- Excel ----------------

    public static byte[] GerarExcel(MapaTurmaDto mapa)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Mapa da Turma");
        var totalColunas = 3 + mapa.Disciplinas.Count * 4 + 1;
        var colunaSituacao = totalColunas;

        ws.Cell(1, 1).Value = "Escola Dois de Julho — Mapa de Notas da Turma";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Turma {mapa.TurmaNome} • {mapa.SerieNome} • {mapa.Turno} • Ano letivo {mapa.AnoLetivo} • " +
                              $"Emitido em {mapa.EmitidoEm.ToString("dd/MM/yyyy HH:mm", PtBr)}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#555555");

        const int linhaCab = 4;
        foreach (var (titulo, coluna) in new[] { ("Nº", 1), ("Matrícula", 2), ("Aluno", 3) })
        {
            ws.Range(linhaCab, coluna, linhaCab + 1, coluna).Merge().Value = titulo;
        }

        for (var d = 0; d < mapa.Disciplinas.Count; d++)
        {
            var inicio = 4 + d * 4;
            var grupo = ws.Range(linhaCab, inicio, linhaCab, inicio + 3).Merge();
            grupo.Value = mapa.Disciplinas[d].Nome;
            for (var i = 0; i < 4; i++)
            {
                ws.Cell(linhaCab + 1, inicio + i).Value = Subcolunas[i];
            }

            if (d % 2 == 1)
            {
                ws.Range(linhaCab, inicio, linhaCab + 1, inicio + 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF9");
            }
        }

        ws.Range(linhaCab, colunaSituacao, linhaCab + 1, colunaSituacao).Merge().Value = "Situação";
        var cabecalho = ws.Range(linhaCab, 1, linhaCab + 1, totalColunas);
        cabecalho.Style.Font.Bold = true;
        cabecalho.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cabecalho.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cabecalho.Style.Alignment.WrapText = true;
        ws.Range(linhaCab, 1, linhaCab + 1, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        ws.Range(linhaCab, colunaSituacao, linhaCab + 1, colunaSituacao).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

        var linha = linhaCab + 2;
        foreach (var aluno in mapa.Alunos)
        {
            ws.Cell(linha, 1).Value = aluno.Numero;
            ws.Cell(linha, 2).Value = aluno.Matricula;
            ws.Cell(linha, 3).Value = aluno.Nome;

            for (var d = 0; d < aluno.Disciplinas.Count; d++)
            {
                var celula = aluno.Disciplinas[d];
                var inicio = 4 + d * 4;
                for (var t = 0; t < 3; t++)
                {
                    EscreverNota(ws.Cell(linha, inicio + t), celula.Trimestres[t]);
                }

                var media = ws.Cell(linha, inicio + 3);
                if (celula.Media.HasValue)
                {
                    media.Value = celula.Media.Value;
                    media.Style.NumberFormat.Format = "0.0";
                    media.Style.Font.Bold = true;
                    if (celula.MediaAbaixo)
                    {
                        media.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                    }
                }

                media.Style.Fill.BackgroundColor = XLColor.FromHtml("#F7F7F7");
            }

            var situacao = ws.Cell(linha, colunaSituacao);
            situacao.Value = aluno.Situacao;
            situacao.Style.Font.FontColor = CorSituacao(aluno);

            if (aluno.Transferido)
            {
                ws.Range(linha, 1, linha, totalColunas).Style.Font.Italic = true;
                ws.Range(linha, 1, linha, colunaSituacao - 1).Style.Font.FontColor = XLColor.FromHtml("#888888");
            }

            linha++;
        }

        var ultimaLinhaAlunos = linha - 1;
        var tabela = ws.Range(linhaCab, 1, ultimaLinhaAlunos, totalColunas);
        tabela.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tabela.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tabela.Style.Border.InsideBorderColor = XLColor.FromHtml("#D0D0D0");
        ws.Range(linhaCab + 2, 4, ultimaLinhaAlunos, colunaSituacao - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Resumo por disciplina
        linha++;
        var linhaResumo = linha;
        var rotulos = new[] { "Média da turma", "Abaixo da média (qtd.)", "Sem nota (qtd.)" };
        for (var r = 0; r < rotulos.Length; r++)
        {
            ws.Range(linha + r, 1, linha + r, 3).Merge().Value = rotulos[r];
        }

        for (var d = 0; d < mapa.Resumo.Count; d++)
        {
            var resumo = mapa.Resumo[d];
            var inicio = 4 + d * 4;
            for (var t = 0; t < 3; t++)
            {
                EscreverDecimal(ws.Cell(linha, inicio + t), resumo.MediaTurmaTrimestres[t]);
                ws.Cell(linha + 1, inicio + t).Value = resumo.AbaixoTrimestres[t];
                ws.Cell(linha + 2, inicio + t).Value = resumo.SemNotaTrimestres[t];
            }

            EscreverDecimal(ws.Cell(linha, inicio + 3), resumo.MediaTurma);
            ws.Cell(linha + 1, inicio + 3).Value = resumo.AbaixoMedia;
        }

        var blocoResumo = ws.Range(linhaResumo, 1, linhaResumo + 2, totalColunas - 1);
        blocoResumo.Style.Font.Bold = true;
        blocoResumo.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        blocoResumo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        blocoResumo.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        blocoResumo.Style.Border.InsideBorderColor = XLColor.FromHtml("#D0D0D0");
        ws.Range(linhaResumo, 4, linhaResumo + 2, totalColunas - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var linhaLegenda = linhaResumo + 4;
        ws.Range(linhaLegenda, 1, linhaLegenda, Math.Max(totalColunas, 12)).Merge().Value = Legenda;
        ws.Row(linhaLegenda).Height = 30;
        ws.Cell(linhaLegenda, 1).Style.Alignment.WrapText = true;
        ws.Cell(linhaLegenda, 1).Style.Font.FontColor = XLColor.FromHtml("#555555");

        ws.Column(1).Width = 5;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 36;
        for (var c = 4; c < colunaSituacao; c++)
        {
            ws.Column(c).Width = 6.5;
        }

        ws.Column(colunaSituacao).Width = 48;
        ws.SheetView.FreezeRows(linhaCab + 1);
        ws.SheetView.FreezeColumns(3);
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(linhaCab, linhaCab + 1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void EscreverNota(IXLCell cell, MapaNotaDto nota)
    {
        switch (nota.Estado)
        {
            case MapaNotaEstado.Pendente:
                cell.Value = "—";
                cell.Style.Font.FontColor = XLColor.FromHtml("#B26A00");
                break;
            case MapaNotaEstado.NaoIniciado:
                break;
            case MapaNotaEstado.Incompleta:
                cell.Value = nota.Valor!.Value;
                cell.Style.NumberFormat.Format = "0.0\"*\"";
                cell.Style.Font.Italic = true;
                cell.Style.Font.FontColor = XLColor.FromHtml("#777777");
                break;
            default:
                cell.Value = nota.Valor!.Value;
                cell.Style.NumberFormat.Format = "0.0";
                if (nota.AbaixoDaMedia)
                {
                    cell.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                    cell.Style.Font.Bold = true;
                }

                break;
        }
    }

    private static void EscreverDecimal(IXLCell cell, decimal? valor)
    {
        if (valor.HasValue)
        {
            cell.Value = valor.Value;
            cell.Style.NumberFormat.Format = "0.0";
        }
    }

    private static XLColor CorSituacao(MapaAlunoDto aluno) =>
        aluno.Transferido ? XLColor.FromHtml("#888888")
        : aluno.AbaixoDaMedia.Count > 0 ? XLColor.FromHtml("#C62828")
        : aluno.NotasPendentes > 0 || aluno.NotasIncompletas > 0 ? XLColor.FromHtml("#B26A00")
        : XLColor.FromHtml("#2E7D32");

    // ---------------- PDF ----------------

    public static byte[] GerarPdf(MapaTurmaDto mapa)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var blocos = mapa.Disciplinas
            .Select((d, i) => (Disciplina: d, Indice: i))
            .Chunk(DisciplinasPorBlocoPdf)
            .ToList();
        if (blocos.Count == 0)
        {
            blocos.Add(Array.Empty<(MapaDisciplinaDto, int)>());
        }

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().PaddingBottom(6).Column(col =>
                {
                    col.Item().Text("Escola Dois de Julho").SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Mapa de Notas da Turma — {mapa.TurmaNome}").Bold().FontSize(11);
                    col.Item().Text($"{mapa.SerieNome} • {mapa.Turno} • Ano letivo {mapa.AnoLetivo}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(col =>
                {
                    for (var b = 0; b < blocos.Count; b++)
                    {
                        var bloco = blocos[b];
                        var ultimo = b == blocos.Count - 1;
                        if (b > 0)
                        {
                            col.Item().PageBreak();
                        }

                        if (blocos.Count > 1)
                        {
                            col.Item().PaddingBottom(3).Text($"Disciplinas — parte {b + 1} de {blocos.Count}").SemiBold().FontSize(8);
                        }

                        col.Item().Element(e => TabelaPdf(e, mapa, bloco, ultimo));
                    }

                    col.Item().PaddingTop(8).Text(Legenda).FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Emitido em {mapa.EmitidoEm.ToString("dd/MM/yyyy HH:mm", PtBr)}").FontSize(7);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.DefaultTextStyle(t => t.FontSize(7));
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void TabelaPdf(IContainer container, MapaTurmaDto mapa, IReadOnlyList<(MapaDisciplinaDto Disciplina, int Indice)> bloco, bool comSituacao)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(18);
                c.RelativeColumn(4);
                foreach (var _ in bloco)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        c.RelativeColumn(1);
                    }
                }

                if (comSituacao)
                {
                    c.RelativeColumn(4.5f);
                }
            });

            IContainer Cab(IContainer cell) => cell.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                .Padding(2).AlignCenter().AlignMiddle();
            IContainer Cel(IContainer cell) => cell.Border(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(1.5f).PaddingHorizontal(2);

            table.Header(h =>
            {
                h.Cell().RowSpan(2).Element(Cab).Text("Nº").Bold();
                h.Cell().RowSpan(2).Element(Cab).Text("Aluno").Bold();
                foreach (var (disciplina, _) in bloco)
                {
                    h.Cell().ColumnSpan(4).Element(Cab).Text(disciplina.Nome).Bold().FontSize(6.5f);
                }

                if (comSituacao)
                {
                    h.Cell().RowSpan(2).Element(Cab).Text("Situação").Bold();
                }

                foreach (var _ in bloco)
                {
                    foreach (var sub in Subcolunas)
                    {
                        h.Cell().Element(Cab).Text(sub).FontSize(6);
                    }
                }
            });

            foreach (var aluno in mapa.Alunos)
            {
                var corBase = aluno.Transferido ? Colors.Grey.Medium : Colors.Black;
                table.Cell().Element(Cel).AlignCenter().Text(aluno.Numero.ToString(PtBr)).FontColor(corBase);
                table.Cell().Element(Cel).Text(t =>
                {
                    var span = t.Span(aluno.Nome).FontColor(corBase);
                    if (aluno.Transferido)
                    {
                        span.Italic();
                    }
                });

                foreach (var (_, indice) in bloco)
                {
                    var celula = aluno.Disciplinas[indice];
                    foreach (var nota in celula.Trimestres)
                    {
                        table.Cell().Element(Cel).AlignCenter().Text(t => EscreverNotaPdf(t, nota));
                    }

                    table.Cell().Element(Cel).Background(Colors.Grey.Lighten4).AlignCenter().Text(t =>
                    {
                        if (celula.Media.HasValue)
                        {
                            var span = t.Span(celula.Media.Value.ToString("0.0", PtBr)).Bold();
                            if (celula.MediaAbaixo)
                            {
                                span.FontColor(Colors.Red.Darken2);
                            }
                        }
                    });
                }

                if (comSituacao)
                {
                    var cor = aluno.Transferido ? Colors.Grey.Medium
                        : aluno.AbaixoDaMedia.Count > 0 ? Colors.Red.Darken2
                        : aluno.NotasPendentes > 0 || aluno.NotasIncompletas > 0 ? Colors.Orange.Darken3
                        : Colors.Green.Darken2;
                    table.Cell().Element(Cel).Text(aluno.Situacao).FontColor(cor).FontSize(6.5f);
                }
            }

            // Resumo por disciplina
            var rotulos = new[] { "Média da turma", "Abaixo da média (qtd.)", "Sem nota (qtd.)" };
            for (var r = 0; r < rotulos.Length; r++)
            {
                table.Cell().ColumnSpan(2).Element(Cab).AlignLeft().Text(rotulos[r]).Bold();
                foreach (var (_, indice) in bloco)
                {
                    var resumo = mapa.Resumo[indice];
                    for (var t = 0; t < 3; t++)
                    {
                        var texto = r switch
                        {
                            0 => resumo.MediaTurmaTrimestres[t]?.ToString("0.0", PtBr) ?? "",
                            1 => resumo.AbaixoTrimestres[t].ToString(PtBr),
                            _ => resumo.SemNotaTrimestres[t].ToString(PtBr),
                        };
                        table.Cell().Element(Cab).Text(texto).Bold();
                    }

                    var textoMedia = r switch
                    {
                        0 => resumo.MediaTurma?.ToString("0.0", PtBr) ?? "",
                        1 => resumo.AbaixoMedia.ToString(PtBr),
                        _ => "",
                    };
                    table.Cell().Element(Cab).Text(textoMedia).Bold();
                }

                if (comSituacao)
                {
                    table.Cell().Element(Cab).Text("");
                }
            }
        });
    }

    private static void EscreverNotaPdf(TextDescriptor t, MapaNotaDto nota)
    {
        switch (nota.Estado)
        {
            case MapaNotaEstado.Pendente:
                t.Span("—").FontColor(Colors.Orange.Darken3);
                break;
            case MapaNotaEstado.NaoIniciado:
                break;
            case MapaNotaEstado.Incompleta:
                t.Span(nota.Valor!.Value.ToString("0.0", PtBr) + "*").Italic().FontColor(Colors.Grey.Darken1);
                break;
            default:
                var span = t.Span(nota.Valor!.Value.ToString("0.0", PtBr));
                if (nota.AbaixoDaMedia)
                {
                    span.FontColor(Colors.Red.Darken2).Bold();
                }

                break;
        }
    }
}
