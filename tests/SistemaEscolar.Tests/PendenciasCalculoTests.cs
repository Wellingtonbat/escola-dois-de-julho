using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Turmas;
using Xunit;

namespace SistemaEscolar.Tests;

public sealed class PendenciasCalculoTests
{
    private const int Ano = 2026;
    private static readonly Guid SerieId = Guid.NewGuid();
    private static readonly Guid PeriodoId = Guid.NewGuid();
    private static readonly TurmaListItemDto Turma = new(Guid.NewGuid(), "6º ANO A", SerieId, "6º Ano", "Matutino", Ano, true);
    private static readonly DisciplinaListItemDto Matematica = Disciplina("Matemática");
    private static readonly DisciplinaListItemDto Historia = Disciplina("História");
    private static readonly ProfessorListItemDto Juliana = Professor("Juliana Moraes", Matematica);

    // 4 alunos ativos e 1 inativo na turma.
    private static readonly AlunoListItemDto[] Alunos =
        Enumerable.Range(1, 4).Select(i => Aluno($"Aluno {i}", ativo: true)).Append(Aluno("Transferido", ativo: false)).ToArray();

    [Fact]
    public void ContaSemNotaIncompletaENaoFinalizada_PorProfessorDisciplinaETurma()
    {
        var notas = new[]
        {
            Nota(Alunos[0], Matematica, 3m, 3m, 3m, finalizada: true),   // completa e finalizada: sem pendência
            Nota(Alunos[1], Matematica, 3m, null, null, finalizada: false), // incompleta e não finalizada
            Nota(Alunos[2], Matematica, 3m, 3m, 3m, finalizada: false),  // só não finalizada
            // Alunos[3] sem nota em Matemática; inativo não conta.
        };

        var itens = PendenciasCalculo.Calcular(Ano, new[] { Turma }, Alunos, new[] { Matematica }, new[] { Juliana }, notas);

        var item = Assert.Single(itens);
        Assert.Equal("Juliana Moraes", item.ProfessorNome);
        Assert.Equal(4, item.TotalAlunos);
        Assert.Equal(1, item.SemNota);
        Assert.Equal(1, item.Incompletas);
        Assert.Equal(2, item.NaoFinalizadas);
    }

    [Fact]
    public void DisciplinaTodaLancadaEFinalizada_NaoApareceNaLista()
    {
        var notas = Alunos.Take(4).Select(a => Nota(a, Matematica, 3m, 3m, 3m, finalizada: true)).ToArray();

        var itens = PendenciasCalculo.Calcular(Ano, new[] { Turma }, Alunos, new[] { Matematica }, new[] { Juliana }, notas);

        Assert.Empty(itens);
    }

    [Fact]
    public void DisciplinaSemProfessor_ApareceComAvisoNoFimDaLista()
    {
        var itens = PendenciasCalculo.Calcular(Ano, new[] { Turma }, Alunos, new[] { Matematica, Historia }, new[] { Juliana }, Array.Empty<NotaListItemDto>());

        Assert.Equal(new[] { "Juliana Moraes", "Sem professor vinculado" }, itens.Select(i => i.ProfessorNome));
        Assert.Equal(4, itens[1].SemNota);
    }

    private static DisciplinaListItemDto Disciplina(string nome) =>
        new(Guid.NewGuid(), nome, nome[..3].ToUpper(), new[] { new DisciplinaSerieDto(SerieId, "6º Ano") }, 80, true);

    private static ProfessorListItemDto Professor(string nome, DisciplinaListItemDto disciplina) =>
        new(Guid.NewGuid(), nome, "p@escola", "00000000000",
            new[] { new ProfessorAtribuicaoDto(Turma.Id, Turma.Nome, "6º Ano", disciplina.Id, disciplina.Nome) }, true);

    private static AlunoListItemDto Aluno(string nome, bool ativo) =>
        new(Guid.NewGuid(), "M", null, "00000000000", nome, new DateTime(2014, 1, 1), Ano, SerieId, "6º Ano", Turma.Id, Turma.Nome, ativo);

    private static NotaListItemDto Nota(AlunoListItemDto aluno, DisciplinaListItemDto disciplina, decimal? av1, decimal? av2, decimal? av3, bool finalizada) =>
        new(Guid.NewGuid(), aluno.Id, aluno.NomeCompleto, disciplina.Id, disciplina.Nome, Juliana.Id, Juliana.NomeCompleto,
            PeriodoId, "1º Trimestre", Ano, 1, av1, av2, av3, null, 0m, 0m, finalizada);
}
