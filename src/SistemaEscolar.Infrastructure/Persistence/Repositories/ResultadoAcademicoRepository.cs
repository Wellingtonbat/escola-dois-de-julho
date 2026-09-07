using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Resultados;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

public sealed class ResultadoAcademicoRepository : IResultadoAcademicoRepository
{
    private const decimal MediaAprovacao = 5.0m;
    private const int BimestresEsperados = 3;

    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ResultadoAcademicoRepository(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ResultadoAcademicoDto>> ListarAsync(ResultadoAcademicoFilter filter, CancellationToken cancellationToken = default)
    {
        var alunosQuery = _context.Alunos
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var disciplinasQuery = _context.Disciplinas
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var seriesQuery = _context.Set<Domain.Entities.Serie>().AsNoTracking();
        var turmasQuery = _context.Set<Domain.Entities.Turma>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Turma))
        {
            var turma = filter.Turma.Trim();
            var turmaIds = await turmasQuery
                .Where(t => t.Nome == turma)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            alunosQuery = alunosQuery.Where(x => x.TurmaId.HasValue && turmaIds.Contains(x.TurmaId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Serie))
        {
            var serie = filter.Serie.Trim();
            var serieIds = await seriesQuery
                .Where(s => s.Nome == serie)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            alunosQuery = alunosQuery.Where(x => serieIds.Contains(x.SerieId));
        }

        var periodoIdsDoAno = _context.PeriodosLancamento
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.AnoLetivo == filter.AnoLetivo)
            .Select(x => x.Id);

        var notasAno = _context.Notas
            .AsNoTracking()
            .Where(x => !x.IsDeleted && periodoIdsDoAno.Contains(x.PeriodoLancamentoId));

        if (_currentUserService.IsInRole("Professor"))
        {
            var cpf = NormalizeCpf(_currentUserService.UserName);
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return Array.Empty<ResultadoAcademicoDto>();
            }

            var professorId = await _context.Professores
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsAtivo && x.UsuarioCpf == cpf)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!professorId.HasValue)
            {
                return Array.Empty<ResultadoAcademicoDto>();
            }

            notasAno = notasAno.Where(x => x.ProfessorId == professorId.Value);
            alunosQuery = alunosQuery.Where(aluno => notasAno.Any(nota => nota.AlunoId == aluno.Id));
        }

        var query =
            from nota in notasAno
            join aluno in alunosQuery on nota.AlunoId equals aluno.Id
            join disciplina in disciplinasQuery on nota.DisciplinaId equals disciplina.Id
            join serie in seriesQuery on aluno.SerieId equals serie.Id
            join turma in turmasQuery on aluno.TurmaId equals (Guid?)turma.Id into turmaGroup
            from turma in turmaGroup.DefaultIfEmpty()
            group nota by new
            {
                aluno.Id,
                aluno.NomeCompleto,
                DisciplinaId = disciplina.Id,
                TurmaNome = turma != null ? turma.Nome : null,
                SerieNome = serie.Nome,
                Disciplina = disciplina.Nome
            }
            into grupo
            select new
            {
                grupo.Key.Id,
                grupo.Key.NomeCompleto,
                grupo.Key.DisciplinaId,
                grupo.Key.TurmaNome,
                grupo.Key.SerieNome,
                grupo.Key.Disciplina,
                TotalLancamentos = grupo.Count(),
                MediaFinal = grupo.Average(x => x.Valor)
            };

        var dados = await query.ToListAsync(cancellationToken);

        var recuperacoesFinais = await _context.RecuperacoesFinais
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.AnoLetivo == filter.AnoLetivo)
            .ToDictionaryAsync(x => (x.AlunoId, x.DisciplinaId), x => x.Valor, cancellationToken);

        var resultados = dados.Select(x =>
        {
            var mediaFinal = Math.Round(x.MediaFinal, 2);
            var completo = x.TotalLancamentos >= BimestresEsperados;
            var recuperacaoFinalDisponivel = completo && mediaFinal < MediaAprovacao;
            var temRecuperacaoFinal = recuperacoesFinais.TryGetValue((x.Id, x.DisciplinaId), out var recuperacaoValor);
            decimal? recuperacaoFinal = temRecuperacaoFinal ? recuperacaoValor : null;

            var resultadoFinalAno = recuperacaoFinal.HasValue && mediaFinal < MediaAprovacao
                ? Math.Max(mediaFinal, recuperacaoFinal.Value)
                : mediaFinal;

            var situacao = !completo
                ? "Pendente"
                : resultadoFinalAno >= MediaAprovacao
                    ? "Aprovado"
                    : "Reprovado";

            var motivo = situacao switch
            {
                "Pendente" => "Lançamentos incompletos no ano letivo.",
                "Reprovado" => recuperacaoFinal.HasValue ? "Recuperação final não atingiu a média mínima." : "Média final abaixo de 5,0.",
                _ => recuperacaoFinal.HasValue && mediaFinal < MediaAprovacao ? "Aprovado por recuperação final." : "Aprovado por média final."
            };

            return new ResultadoAcademicoDto(
                x.Id,
                x.NomeCompleto,
                x.DisciplinaId,
                x.Disciplina,
                x.TurmaNome ?? string.Empty,
                x.SerieNome,
                filter.AnoLetivo,
                mediaFinal,
                recuperacaoFinal,
                recuperacaoFinalDisponivel,
                Math.Round(resultadoFinalAno, 2),
                situacao,
                motivo);
        });

        if (!string.Equals(filter.Situacao, "todos", StringComparison.OrdinalIgnoreCase))
        {
            resultados = resultados.Where(x => x.Situacao.Equals(filter.Situacao, StringComparison.OrdinalIgnoreCase));
        }

        return resultados
            .OrderBy(x => x.Disciplina)
            .ThenBy(x => x.AlunoNome)
            .ToList();
    }

    private static string NormalizeCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        return new string(cpf.Where(char.IsDigit).ToArray());
    }
}