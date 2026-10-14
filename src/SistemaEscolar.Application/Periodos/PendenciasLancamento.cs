using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Application.Periodos;

// Uma linha da lista de pendências de um período: um professor numa disciplina de uma turma.
public sealed record PendenciaLancamentoDto(
    string ProfessorNome,
    string DisciplinaNome,
    string TurmaNome,
    int TotalAlunos,
    // Aluno ativo da turma sem nenhuma nota no trimestre.
    int SemNota,
    // Nota lançada, mas faltando alguma das 3 avaliações.
    int Incompletas,
    // Nota lançada que ainda não foi marcada como finalizada.
    int NaoFinalizadas)
{
    public int TotalPendencias => SemNota + Incompletas + NaoFinalizadas;
}

public sealed record PendenciasPeriodoDto(
    Guid PeriodoId,
    string PeriodoDescricao,
    int AnoLetivo,
    IReadOnlyList<PendenciaLancamentoDto> Itens)
{
    public bool TemPendencias => Itens.Count > 0;

    // A descrição costuma já trazer o ano ("1º Trimestre 2026"); só acrescenta quando não traz.
    public string PeriodoNome => PeriodoDescricao.Contains(AnoLetivo.ToString(System.Globalization.CultureInfo.InvariantCulture))
        ? PeriodoDescricao
        : $"{PeriodoDescricao} / {AnoLetivo}";
}

public interface IPendenciasLancamentoService
{
    // Nulo quando o período não existe ou quem pede não pode abrir/fechar períodos.
    Task<PendenciasPeriodoDto?> ListarAsync(Guid periodoId, CancellationToken cancellationToken = default);
}

public sealed class PendenciasLancamentoService : IPendenciasLancamentoService
{
    private readonly IPeriodoRepository _periodoRepository;
    private readonly INotaRepository _notaRepository;
    private readonly IAlunoService _alunoService;
    private readonly ITurmaService _turmaService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly IProfessorService _professorService;
    private readonly ICurrentUserService _currentUserService;

    public PendenciasLancamentoService(
        IPeriodoRepository periodoRepository,
        INotaRepository notaRepository,
        IAlunoService alunoService,
        ITurmaService turmaService,
        IDisciplinaService disciplinaService,
        IProfessorService professorService,
        ICurrentUserService currentUserService)
    {
        _periodoRepository = periodoRepository;
        _notaRepository = notaRepository;
        _alunoService = alunoService;
        _turmaService = turmaService;
        _disciplinaService = disciplinaService;
        _professorService = professorService;
        _currentUserService = currentUserService;
    }

    public async Task<PendenciasPeriodoDto?> ListarAsync(Guid periodoId, CancellationToken cancellationToken = default)
    {
        if (!PermissoesPerfil.PodeAbrirFecharPeriodo(_currentUserService.IsInRole))
        {
            return null;
        }

        var periodo = await _periodoRepository.GetByIdAsync(periodoId, cancellationToken);
        if (periodo is null)
        {
            return null;
        }

        var turmas = (await _turmaService.ListarAsync(new TurmaListFilter(null, null, periodo.AnoLetivo, true), cancellationToken))
            .Where(t => t.AnoLetivo == periodo.AnoLetivo)
            .ToList();
        var alunos = await _alunoService.ListarAsync(null, cancellationToken);
        var disciplinas = (await _disciplinaService.ListarAsync(null, cancellationToken)).Where(d => d.IsAtiva).ToList();
        var professores = (await _professorService.ListarAsync(null, cancellationToken)).Where(p => p.IsAtivo).ToList();
        var notas = (await _notaRepository.GetAllAsync(new NotaListFilter(null, periodo.AnoLetivo, null, null), cancellationToken))
            .Where(n => n.PeriodoLancamentoId == periodo.Id)
            .ToList();

        var itens = PendenciasCalculo.Calcular(periodo.AnoLetivo, turmas, alunos, disciplinas, professores, notas);
        return new PendenciasPeriodoDto(periodo.Id, periodo.Descricao, periodo.AnoLetivo, itens);
    }
}

// Regra das pendências, sem acesso a banco (testável isoladamente). Considera os alunos ativos de cada turma
// ativa do ano e as disciplinas ativas da série da turma — o mesmo "esperado" do Mapa de Pendências do Painel.
public static class PendenciasCalculo
{
    private const string SemProfessor = "Sem professor vinculado";

    public static IReadOnlyList<PendenciaLancamentoDto> Calcular(
        int anoLetivo,
        IReadOnlyList<TurmaListItemDto> turmas,
        IReadOnlyList<AlunoListItemDto> alunos,
        IReadOnlyList<DisciplinaListItemDto> disciplinas,
        IReadOnlyList<ProfessorListItemDto> professores,
        IReadOnlyList<NotaListItemDto> notasDoPeriodo)
    {
        var alunosPorTurma = alunos
            .Where(a => a.IsAtivo && a.AnoLetivo == anoLetivo && a.TurmaId.HasValue)
            .GroupBy(a => a.TurmaId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(a => a.Id).ToHashSet());
        var notasPorAlunoDisciplina = notasDoPeriodo
            .GroupBy(n => (n.AlunoId, n.DisciplinaId))
            .ToDictionary(g => g.Key, g => g.First());

        var itens = new List<PendenciaLancamentoDto>();
        foreach (var turma in turmas)
        {
            if (!alunosPorTurma.TryGetValue(turma.Id, out var alunosDaTurma) || alunosDaTurma.Count == 0)
            {
                continue;
            }

            foreach (var disciplina in disciplinas.Where(d => d.Series.Any(s => s.SerieId == turma.SerieId)))
            {
                var semNota = 0;
                var incompletas = 0;
                var naoFinalizadas = 0;
                foreach (var alunoId in alunosDaTurma)
                {
                    if (!notasPorAlunoDisciplina.TryGetValue((alunoId, disciplina.Id), out var nota))
                    {
                        semNota++;
                        continue;
                    }

                    if (!nota.Avaliacao1.HasValue || !nota.Avaliacao2.HasValue || !nota.Avaliacao3.HasValue)
                    {
                        incompletas++;
                    }

                    if (!nota.IsFinalizada)
                    {
                        naoFinalizadas++;
                    }
                }

                if (semNota + incompletas + naoFinalizadas == 0)
                {
                    continue;
                }

                var nomesProfessores = professores
                    .Where(p => p.Atribuicoes.Any(a => a.TurmaId == turma.Id && a.DisciplinaId == disciplina.Id))
                    .Select(p => p.NomeCompleto)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var professorNome = nomesProfessores.Count > 0 ? string.Join(", ", nomesProfessores) : SemProfessor;

                itens.Add(new PendenciaLancamentoDto(
                    professorNome, disciplina.Nome, turma.Nome, alunosDaTurma.Count, semNota, incompletas, naoFinalizadas));
            }
        }

        // "Sem professor vinculado" por último: é um aviso de cadastro, não de lançamento.
        return itens
            .OrderBy(i => i.ProfessorNome == SemProfessor)
            .ThenBy(i => i.ProfessorNome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.DisciplinaNome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.TurmaNome, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
