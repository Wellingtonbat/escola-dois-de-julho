using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Domain.Entities;
using SistemaEscolar.Infrastructure.Persistence;
using SistemaEscolar.Infrastructure.Persistence.Repositories;
using SistemaEscolar.Tests.Support;

namespace SistemaEscolar.Tests;

public sealed class ResultadoAcademicoRepositoryTests
{
    [Fact]
    public async Task ListarAsync_ProfessorVeSomenteSeusLancements()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var professorA = new Professor
        {
            Id = Guid.NewGuid(),
            NomeCompleto = "Professor A",
            Email = "a@escola.local",
            UsuarioCpf = "12345678901",
            IsAtivo = true
        };

        var professorB = new Professor
        {
            Id = Guid.NewGuid(),
            NomeCompleto = "Professor B",
            Email = "b@escola.local",
            UsuarioCpf = "98765432100",
            IsAtivo = true
        };

        var serie8Ano = new Serie { Id = Guid.NewGuid(), Nome = "8o Ano", Ordem = 8, IsAtiva = true };
        var serie9Ano = new Serie { Id = Guid.NewGuid(), Nome = "9o Ano", Ordem = 9, IsAtiva = true };

        var alunoA = new Aluno
        {
            Id = Guid.NewGuid(),
            Matricula = "100001",
            Cpf = "11111111111",
            NomeCompleto = "Aluno A",
            DataNascimento = new DateTime(2012, 1, 1),
            AnoLetivo = 2026,
            Turma = "8o Ano A",
            SerieId = serie8Ano.Id,
            IsAtivo = true
        };

        var alunoB = new Aluno
        {
            Id = Guid.NewGuid(),
            Matricula = "100002",
            Cpf = "22222222222",
            NomeCompleto = "Aluno B",
            DataNascimento = new DateTime(2012, 2, 1),
            AnoLetivo = 2026,
            Turma = "9o Ano B",
            SerieId = serie9Ano.Id,
            IsAtivo = true
        };

        var disciplinaA = new Disciplina
        {
            Id = Guid.NewGuid(),
            Nome = "Matemática",
            Codigo = "MAT-01",
            CargaHoraria = 80,
            IsAtiva = true
        };

        var disciplinaB = new Disciplina
        {
            Id = Guid.NewGuid(),
            Nome = "História",
            Codigo = "HIS-01",
            CargaHoraria = 80,
            IsAtiva = true
        };

        var periodo1 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 1, Descricao = "1o Bimestre 2026", DataInicial = new DateTime(2026, 2, 1), DataFinal = new DateTime(2026, 3, 31), IsAberto = true };
        var periodo2 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 2, Descricao = "2o Bimestre 2026", DataInicial = new DateTime(2026, 4, 1), DataFinal = new DateTime(2026, 5, 31), IsAberto = true };
        var periodo3 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 3, Descricao = "3o Bimestre 2026", DataInicial = new DateTime(2026, 6, 1), DataFinal = new DateTime(2026, 7, 31), IsAberto = true };
        var periodo4 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 4, Descricao = "4o Bimestre 2026", DataInicial = new DateTime(2026, 8, 1), DataFinal = new DateTime(2026, 9, 30), IsAberto = true };

        context.Professores.AddRange(professorA, professorB);
        context.Series.AddRange(serie8Ano, serie9Ano);
        context.Alunos.AddRange(alunoA, alunoB);
        context.Disciplinas.AddRange(disciplinaA, disciplinaB);
        context.PeriodosLancamento.AddRange(periodo1, periodo2, periodo3, periodo4);

        context.Notas.AddRange(
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplinaA.Id,
                ProfessorId = professorA.Id,
                PeriodoLancamentoId = periodo1.Id,
                Valor = 8,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplinaA.Id,
                ProfessorId = professorA.Id,
                PeriodoLancamentoId = periodo2.Id,
                Valor = 7,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplinaA.Id,
                ProfessorId = professorA.Id,
                PeriodoLancamentoId = periodo3.Id,
                Valor = 8,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplinaA.Id,
                ProfessorId = professorA.Id,
                PeriodoLancamentoId = periodo4.Id,
                Valor = 9,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplinaB.Id,
                ProfessorId = professorB.Id,
                PeriodoLancamentoId = periodo1.Id,
                Valor = 5,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplinaB.Id,
                ProfessorId = professorB.Id,
                PeriodoLancamentoId = periodo2.Id,
                Valor = 5,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplinaB.Id,
                ProfessorId = professorB.Id,
                PeriodoLancamentoId = periodo3.Id,
                Valor = 5,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplinaB.Id,
                ProfessorId = professorB.Id,
                PeriodoLancamentoId = periodo4.Id,
                Valor = 5,
                IsFinalizada = true
            });

        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService("123.456.789-01", "Professor");
        var repository = new ResultadoAcademicoRepository(context, currentUser);

        var result = await repository.ListarAsync(new ResultadoAcademicoFilter(2026, null, null, "todos"));

        Assert.Single(result);
        Assert.Equal("Aluno A", result[0].AlunoNome);
        Assert.Equal("Aprovado", result[0].Situacao);
    }

    [Fact]
    public async Task ListarAsync_DeveAplicarFiltrosCombinados_DeTurmaSerieESituacao()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var professor = new Professor
        {
            Id = Guid.NewGuid(),
            NomeCompleto = "Professor A",
            Email = "a@escola.local",
            UsuarioCpf = "12345678901",
            IsAtivo = true
        };

        var serie8Ano = new Serie { Id = Guid.NewGuid(), Nome = "8o Ano", Ordem = 8, IsAtiva = true };
        var serie9Ano = new Serie { Id = Guid.NewGuid(), Nome = "9o Ano", Ordem = 9, IsAtiva = true };

        var alunoA = new Aluno
        {
            Id = Guid.NewGuid(),
            Matricula = "100001",
            Cpf = "11111111111",
            NomeCompleto = "Aluno A",
            DataNascimento = new DateTime(2012, 1, 1),
            AnoLetivo = 2026,
            Turma = "8o Ano A",
            SerieId = serie8Ano.Id,
            IsAtivo = true
        };

        var alunoB = new Aluno
        {
            Id = Guid.NewGuid(),
            Matricula = "100002",
            Cpf = "22222222222",
            NomeCompleto = "Aluno B",
            DataNascimento = new DateTime(2012, 2, 1),
            AnoLetivo = 2026,
            Turma = "9o Ano B",
            SerieId = serie9Ano.Id,
            IsAtivo = true
        };

        var disciplina = new Disciplina
        {
            Id = Guid.NewGuid(),
            Nome = "Matemática",
            Codigo = "MAT-01",
            CargaHoraria = 80,
            IsAtiva = true
        };

        var periodo1 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 1, Descricao = "1o Bimestre 2026", DataInicial = new DateTime(2026, 2, 1), DataFinal = new DateTime(2026, 3, 31), IsAberto = true };
        var periodo2 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 2, Descricao = "2o Bimestre 2026", DataInicial = new DateTime(2026, 4, 1), DataFinal = new DateTime(2026, 5, 31), IsAberto = true };
        var periodo3 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 3, Descricao = "3o Bimestre 2026", DataInicial = new DateTime(2026, 6, 1), DataFinal = new DateTime(2026, 7, 31), IsAberto = true };
        var periodo4 = new PeriodoLancamento { Id = Guid.NewGuid(), AnoLetivo = 2026, Bimestre = 4, Descricao = "4o Bimestre 2026", DataInicial = new DateTime(2026, 8, 1), DataFinal = new DateTime(2026, 9, 30), IsAberto = true };

        context.Professores.Add(professor);
        context.Series.AddRange(serie8Ano, serie9Ano);
        context.Alunos.AddRange(alunoA, alunoB);
        context.Disciplinas.Add(disciplina);
        context.PeriodosLancamento.AddRange(periodo1, periodo2, periodo3, periodo4);

        context.Notas.AddRange(
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo1.Id,
                Valor = 8,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo2.Id,
                Valor = 7,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo3.Id,
                Valor = 8,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoA.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo4.Id,
                Valor = 9,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo1.Id,
                Valor = 5,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo2.Id,
                Valor = 5,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo3.Id,
                Valor = 5,
                IsFinalizada = true
            },
            new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoB.Id,
                DisciplinaId = disciplina.Id,
                ProfessorId = professor.Id,
                PeriodoLancamentoId = periodo4.Id,
                Valor = 5,
                IsFinalizada = true
            });

        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService(null);
        var repository = new ResultadoAcademicoRepository(context, currentUser);

        var result = await repository.ListarAsync(new ResultadoAcademicoFilter(2026, "8o Ano A", "8o Ano", "Aprovado"));

        Assert.Single(result);
        Assert.Equal("Aluno A", result[0].AlunoNome);
        Assert.Equal("Aprovado", result[0].Situacao);
    }
}
