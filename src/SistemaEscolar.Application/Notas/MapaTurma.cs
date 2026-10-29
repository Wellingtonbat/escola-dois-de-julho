using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Application.Notas;

// Situação de uma nota no mapa: lançada (completa ou não), esperada mas ainda sem lançamento, ou de um
// trimestre que ainda não começou (não é pendência).
public enum MapaNotaEstado
{
    Lancada,
    Incompleta,
    Pendente,
    NaoIniciado,
}

public sealed record MapaNotaDto(decimal? Valor, MapaNotaEstado Estado)
{
    public bool AbaixoDaMedia => Estado == MapaNotaEstado.Lancada && Valor < MapaTurmaCalculo.MediaAprovacao;
}

// Uma disciplina de um aluno: os 3 trimestres e a média parcial (média dos trimestres com lançamento completo;
// uma nota incompleta ainda vai mudar e distorceria a média).
public sealed record MapaCelulaDto(IReadOnlyList<MapaNotaDto> Trimestres, decimal? Media)
{
    public bool MediaAbaixo => Media < MapaTurmaCalculo.MediaAprovacao;
}

public sealed record MapaDisciplinaDto(Guid Id, string Nome, string Codigo);

public sealed record MapaAlunoDto(
    int Numero,
    string Matricula,
    string Nome,
    bool Transferido,
    IReadOnlyList<MapaCelulaDto> Disciplinas,
    IReadOnlyList<string> AbaixoDaMedia,
    int NotasPendentes,
    int NotasIncompletas,
    string Situacao);

// Rodapé de uma disciplina: média da turma, quantos abaixo da média e quantos sem nota, por trimestre e na média.
public sealed record MapaResumoDisciplinaDto(
    IReadOnlyList<decimal?> MediaTurmaTrimestres,
    decimal? MediaTurma,
    IReadOnlyList<int> AbaixoTrimestres,
    int AbaixoMedia,
    IReadOnlyList<int> SemNotaTrimestres);

public sealed record MapaTurmaDto(
    string TurmaNome,
    string SerieNome,
    string Turno,
    int AnoLetivo,
    IReadOnlyList<string> Trimestres,
    IReadOnlyList<MapaDisciplinaDto> Disciplinas,
    IReadOnlyList<MapaAlunoDto> Alunos,
    IReadOnlyList<MapaResumoDisciplinaDto> Resumo,
    DateTime EmitidoEm);

public sealed record MapaTurmaResult(bool Succeeded, string? ErrorMessage, MapaTurmaDto? Mapa)
{
    public static MapaTurmaResult Success(MapaTurmaDto mapa) => new(true, null, mapa);

    public static MapaTurmaResult Fail(string erro) => new(false, erro, null);
}

public interface IMapaTurmaService
{
    Task<MapaTurmaResult> GerarAsync(Guid turmaId, CancellationToken cancellationToken = default);
}

public sealed class MapaTurmaService : IMapaTurmaService
{
    private readonly ITurmaService _turmaService;
    private readonly IAlunoService _alunoService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IPeriodoService _periodoService;
    private readonly INotaRepository _notaRepository;
    private readonly IProfessorService _professorService;
    private readonly ICurrentUserService _currentUserService;

    public MapaTurmaService(
        ITurmaService turmaService,
        IAlunoService alunoService,
        IDisciplinaService disciplinaService,
        IPeriodoService periodoService,
        INotaRepository notaRepository,
        IProfessorService professorService,
        ICurrentUserService currentUserService)
    {
        _turmaService = turmaService;
        _alunoService = alunoService;
        _disciplinaService = disciplinaService;
        _periodoService = periodoService;
        _notaRepository = notaRepository;
        _professorService = professorService;
        _currentUserService = currentUserService;
    }

    public async Task<MapaTurmaResult> GerarAsync(Guid turmaId, CancellationToken cancellationToken = default)
    {
        if (!PermissoesPerfil.PodeGerarMapaTurma(_currentUserService.TemPermissao))
        {
            return MapaTurmaResult.Fail("Seu perfil não tem permissão para gerar o mapa da turma.");
        }

        var turma = await _turmaService.ObterPorIdAsync(turmaId, cancellationToken);
        if (turma is null)
        {
            return MapaTurmaResult.Fail("Turma não encontrada.");
        }

        // Visão restrita (professor): só as próprias turmas e, nelas, só as próprias disciplinas.
        HashSet<Guid>? disciplinasPermitidas = null;
        if (PermissoesPerfil.TemVisaoRestrita(_currentUserService.TemPermissao))
        {
            var escopo = await _professorService.ObterEscopoPorUsuarioAsync(_currentUserService.UserName, cancellationToken);
            var professor = escopo is null ? null : await _professorService.ObterPorIdAsync(escopo.ProfessorId, cancellationToken);
            disciplinasPermitidas = (professor?.Atribuicoes ?? Array.Empty<ProfessorAtribuicaoDto>())
                .Where(a => a.TurmaId == turma.Id)
                .Select(a => a.DisciplinaId)
                .ToHashSet();
            if (disciplinasPermitidas.Count == 0)
            {
                return MapaTurmaResult.Fail("Você só pode gerar o mapa das turmas em que é professor.");
            }
        }

        var periodos = (await _periodoService.ListarAsync(null, cancellationToken))
            .Where(p => p.AnoLetivo == turma.AnoLetivo)
            .ToList();
        var alunos = await _alunoService.ListarAsync(new AlunoListFilter(null, null, turma.Id, null), cancellationToken);
        var disciplinas = await _disciplinaService.ListarAsync(null, cancellationToken);
        var notas = await _notaRepository.GetAllAsync(new NotaListFilter(null, turma.AnoLetivo, null, null, turma.Id), cancellationToken);

        var mapa = MapaTurmaCalculo.Montar(turma, periodos, alunos, disciplinas, notas, HorarioBrasilia.Agora, disciplinasPermitidas);
        return MapaTurmaResult.Success(mapa);
    }
}

// Montagem do mapa, sem acesso a banco (testável isoladamente). Usa as mesmas regras do boletim: a nota do
// trimestre é o resultado final da unidade (maior entre a soma das avaliações e a recuperação paralela) e a
// média é a dos trimestres já lançados.
public static class MapaTurmaCalculo
{
    public const decimal MediaAprovacao = 5.0m;
    private const int Trimestres = 3;

    public static MapaTurmaDto Montar(
        TurmaListItemDto turma,
        IReadOnlyList<PeriodoListItemDto> periodosDoAno,
        IReadOnlyList<AlunoListItemDto> alunosDaTurma,
        IReadOnlyList<DisciplinaListItemDto> disciplinas,
        IReadOnlyList<NotaListItemDto> notas,
        DateTime agora,
        IReadOnlySet<Guid>? disciplinasPermitidas = null)
    {
        var notasDaTurma = notas.Where(n => n.AnoLetivo == turma.AnoLetivo).ToList();
        var disciplinasComNota = notasDaTurma.Select(n => n.DisciplinaId).ToHashSet();

        // Colunas: disciplinas ativas da série da turma, mais qualquer outra que já tenha nota lançada.
        var colunas = disciplinas
            .Where(d => (d.IsAtiva && d.Series.Any(s => s.SerieId == turma.SerieId)) || disciplinasComNota.Contains(d.Id))
            .Where(d => disciplinasPermitidas is null || disciplinasPermitidas.Contains(d.Id))
            .OrderBy(d => d.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(d => new MapaDisciplinaDto(d.Id, d.Nome, string.IsNullOrWhiteSpace(d.Codigo) ? d.Nome : d.Codigo))
            .ToList();

        // Um trimestre só gera pendência depois que começa; sem período cadastrado, ainda não começou.
        var periodoPorTrimestre = Enumerable.Range(1, Trimestres)
            .Select(t => periodosDoAno.FirstOrDefault(p => p.Trimestre == t))
            .ToList();
        var iniciado = periodoPorTrimestre.Select(p => p is not null && p.DataInicial.Date <= agora.Date).ToList();

        var notasPorAlunoDisciplina = notasDaTurma
            .GroupBy(n => (n.AlunoId, n.DisciplinaId))
            .ToDictionary(g => g.Key, g => g.ToList());

        var alunos = new List<MapaAlunoDto>();
        var numero = 0;
        foreach (var aluno in alunosDaTurma.OrderBy(a => a.NomeCompleto, StringComparer.CurrentCultureIgnoreCase))
        {
            numero++;
            if (!aluno.IsAtivo)
            {
                var vazias = colunas
                    .Select(_ => new MapaCelulaDto(Enumerable.Repeat(new MapaNotaDto(null, MapaNotaEstado.NaoIniciado), Trimestres).ToList(), null))
                    .ToList();
                alunos.Add(new MapaAlunoDto(numero, aluno.Matricula, aluno.NomeCompleto, true, vazias, Array.Empty<string>(), 0, 0, "Transferido(a)"));
                continue;
            }

            var celulas = new List<MapaCelulaDto>();
            var abaixo = new List<string>();
            var pendentes = 0;
            var incompletas = 0;
            foreach (var coluna in colunas)
            {
                var doAluno = notasPorAlunoDisciplina.GetValueOrDefault((aluno.Id, coluna.Id)) ?? new List<NotaListItemDto>();
                var trimestres = new List<MapaNotaDto>();
                for (var t = 0; t < Trimestres; t++)
                {
                    var periodo = periodoPorTrimestre[t];
                    var nota = periodo is null ? null : doAluno.FirstOrDefault(n => n.PeriodoLancamentoId == periodo.Id);
                    trimestres.Add(Classificar(nota, iniciado[t]));
                }

                pendentes += trimestres.Count(x => x.Estado == MapaNotaEstado.Pendente);
                incompletas += trimestres.Count(x => x.Estado == MapaNotaEstado.Incompleta);
                var lancadas = trimestres.Where(x => x.Estado == MapaNotaEstado.Lancada).ToList();
                decimal? media = lancadas.Count == 0 ? null : Math.Round(lancadas.Average(x => x.Valor!.Value), 2);
                var celula = new MapaCelulaDto(trimestres, media);
                if (celula.MediaAbaixo)
                {
                    abaixo.Add(coluna.Codigo);
                }

                celulas.Add(celula);
            }

            alunos.Add(new MapaAlunoDto(numero, aluno.Matricula, aluno.NomeCompleto, false, celulas, abaixo, pendentes, incompletas,
                DescreverSituacao(abaixo, pendentes, incompletas)));
        }

        var ativos = alunos.Where(a => !a.Transferido).ToList();
        var resumo = colunas.Select((_, i) =>
        {
            var celulasDaColuna = ativos.Select(a => a.Disciplinas[i]).ToList();
            var mediasTrimestre = new List<decimal?>();
            var abaixoTrimestre = new List<int>();
            var semNota = new List<int>();
            for (var t = 0; t < Trimestres; t++)
            {
                var notasDoTrimestre = celulasDaColuna.Select(c => c.Trimestres[t]).ToList();
                var lancadas = notasDoTrimestre.Where(n => n.Estado == MapaNotaEstado.Lancada).ToList();
                mediasTrimestre.Add(lancadas.Count == 0 ? null : Math.Round(lancadas.Average(n => n.Valor!.Value), 2));
                abaixoTrimestre.Add(notasDoTrimestre.Count(n => n.AbaixoDaMedia));
                semNota.Add(notasDoTrimestre.Count(n => n.Estado == MapaNotaEstado.Pendente));
            }

            var medias = celulasDaColuna.Where(c => c.Media.HasValue).Select(c => c.Media!.Value).ToList();
            return new MapaResumoDisciplinaDto(
                mediasTrimestre,
                medias.Count == 0 ? null : Math.Round(medias.Average(), 2),
                abaixoTrimestre,
                celulasDaColuna.Count(c => c.MediaAbaixo),
                semNota);
        }).ToList();

        var nomesTrimestres = Enumerable.Range(1, Trimestres).Select(t => $"{t}º Trimestre").ToList();

        return new MapaTurmaDto(turma.Nome, turma.SerieNome, turma.Turno, turma.AnoLetivo, nomesTrimestres, colunas, alunos, resumo, agora);
    }

    private static MapaNotaDto Classificar(NotaListItemDto? nota, bool trimestreIniciado)
    {
        var avaliacoes = nota is null ? 0 : new[] { nota.Avaliacao1, nota.Avaliacao2, nota.Avaliacao3 }.Count(x => x.HasValue);
        if (nota is null || avaliacoes == 0)
        {
            return new MapaNotaDto(null, trimestreIniciado ? MapaNotaEstado.Pendente : MapaNotaEstado.NaoIniciado);
        }

        // Faltando avaliação (e sem recuperação paralela que a substitua), a nota ainda vai mudar: não é marcada
        // como abaixo da média, só sinalizada como incompleta.
        var incompleta = avaliacoes < 3 && !nota.RecuperacaoParalela.HasValue;
        return new MapaNotaDto(nota.ResultadoFinalUnidade, incompleta ? MapaNotaEstado.Incompleta : MapaNotaEstado.Lancada);
    }

    private static string DescreverSituacao(IReadOnlyList<string> abaixo, int pendentes, int incompletas)
    {
        var partes = new List<string>();
        if (abaixo.Count > 0)
        {
            partes.Add($"Abaixo da média em {abaixo.Count}: {string.Join(", ", abaixo)}");
        }

        if (pendentes > 0)
        {
            partes.Add(pendentes == 1 ? "1 nota não lançada" : $"{pendentes} notas não lançadas");
        }

        if (incompletas > 0)
        {
            partes.Add(incompletas == 1 ? "1 nota incompleta" : $"{incompletas} notas incompletas");
        }

        return partes.Count == 0 ? "Em dia" : string.Join("; ", partes);
    }
}
