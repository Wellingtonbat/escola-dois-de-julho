using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Infrastructure.Identity;

namespace SistemaEscolar.Infrastructure.Persistence;

public sealed class ApplicationDbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IAlunoRepository _alunoRepository;
    private readonly IAccountProvisioner _professorAccountProvisioner;

    public ApplicationDbInitializer(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IAlunoRepository alunoRepository,
        IAccountProvisioner professorAccountProvisioner)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _alunoRepository = alunoRepository;
        _professorAccountProvisioner = professorAccountProvisioner;
    }

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
    }

    public async Task SeedAsync(bool seedSampleData, CancellationToken cancellationToken = default)
    {
        var roles = new[] { "Diretor", "Coordenador", "Secretaria", "Professor" };

        foreach (var roleName in roles)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var role = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };

            await _roleManager.CreateAsync(role);
        }

        if (seedSampleData)
        {
            await SeedSeriesAsync(cancellationToken);
            await SeedDisciplinasAsync(cancellationToken);
            await SeedTurmasAsync(cancellationToken);
            await SeedAlunosAsync(cancellationToken);
            await SeedProfessoresAsync(cancellationToken);
            await SeedPeriodosAsync(cancellationToken);
            await SeedNotasAsync(cancellationToken);
        }

        await SeedDirectorUserAsync();
    }

    private async Task SeedAlunosAsync(CancellationToken cancellationToken)
    {
        if (await _context.Alunos.AnyAsync(cancellationToken))
        {
            return;
        }

        var serie8Ano = await _context.Series.FirstOrDefaultAsync(x => x.Nome == "8o Ano", cancellationToken);
        var serie9Ano = await _context.Series.FirstOrDefaultAsync(x => x.Nome == "9o Ano", cancellationToken);
        var serie7Ano = await _context.Series.FirstOrDefaultAsync(x => x.Nome == "7o Ano", cancellationToken);

        if (serie8Ano is null || serie9Ano is null || serie7Ano is null)
        {
            return;
        }

        var turma8AnoA = await _context.Turmas.FirstOrDefaultAsync(x => x.Nome == "8o Ano A", cancellationToken);
        var turma9AnoB = await _context.Turmas.FirstOrDefaultAsync(x => x.Nome == "9o Ano B", cancellationToken);
        var turma7AnoC = await _context.Turmas.FirstOrDefaultAsync(x => x.Nome == "7o Ano C", cancellationToken);

        var proximaMatricula = await _alunoRepository.ProximaMatriculaAsync(cancellationToken);
        var alunos = new[]
        {
            new Domain.Entities.Aluno
            {
                Id = Guid.NewGuid(),
                Matricula = proximaMatricula,
                Cpf = "11111111111",
                NomeCompleto = "Gabriel Santos",
                DataNascimento = new DateTime(2012, 4, 10),
                AnoLetivo = 2026,
                TurmaId = turma8AnoA?.Id,
                SerieId = serie8Ano.Id,
                IsAtivo = true
            },
            new Domain.Entities.Aluno
            {
                Id = Guid.NewGuid(),
                Matricula = await _alunoRepository.ProximaMatriculaAsync(cancellationToken),
                Cpf = "22222222222",
                NomeCompleto = "Ana Beatriz Lima",
                DataNascimento = new DateTime(2011, 9, 18),
                AnoLetivo = 2026,
                TurmaId = turma9AnoB?.Id,
                SerieId = serie9Ano.Id,
                IsAtivo = true
            },
            new Domain.Entities.Aluno
            {
                Id = Guid.NewGuid(),
                Matricula = await _alunoRepository.ProximaMatriculaAsync(cancellationToken),
                Cpf = "33333333333",
                NomeCompleto = "Lucas Ferreira",
                DataNascimento = new DateTime(2013, 1, 30),
                AnoLetivo = 2026,
                TurmaId = turma7AnoC?.Id,
                SerieId = serie7Ano.Id,
                IsAtivo = false
            }
        };

        _context.Alunos.AddRange(alunos);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSeriesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Series.AnyAsync(cancellationToken))
        {
            return;
        }

        var series = new[]
        {
            new Domain.Entities.Serie { Id = Guid.NewGuid(), Nome = "1o Ano", Ordem = 1, IsAtiva = true },
            new Domain.Entities.Serie { Id = Guid.NewGuid(), Nome = "2o Ano", Ordem = 2, IsAtiva = true },
            new Domain.Entities.Serie { Id = Guid.NewGuid(), Nome = "3o Ano", Ordem = 3, IsAtiva = true },
            new Domain.Entities.Serie { Id = Guid.NewGuid(), Nome = "6o Ano", Ordem = 6, IsAtiva = true },
            new Domain.Entities.Serie { Id = Guid.NewGuid(), Nome = "7o Ano", Ordem = 7, IsAtiva = true },
            new Domain.Entities.Serie { Id = Guid.NewGuid(), Nome = "8o Ano", Ordem = 8, IsAtiva = true },
            new Domain.Entities.Serie { Id = Guid.NewGuid(), Nome = "9o Ano", Ordem = 9, IsAtiva = true }
        };

        _context.Series.AddRange(series);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDisciplinasAsync(CancellationToken cancellationToken)
    {
        if (await _context.Disciplinas.AnyAsync(cancellationToken))
        {
            return;
        }

        var seriesDoFundamental2 = await _context.Series
            .Where(x => new[] { "6o Ano", "7o Ano", "8o Ano", "9o Ano" }.Contains(x.Nome))
            .ToListAsync(cancellationToken);

        var disciplinas = new[]
        {
            new Domain.Entities.Disciplina { Id = Guid.NewGuid(), Nome = "Matemática", Codigo = "MAT-01", CargaHoraria = 80, IsAtiva = true },
            new Domain.Entities.Disciplina { Id = Guid.NewGuid(), Nome = "História", Codigo = "HIS-01", CargaHoraria = 60, IsAtiva = true },
            new Domain.Entities.Disciplina { Id = Guid.NewGuid(), Nome = "Português", Codigo = "POR-01", CargaHoraria = 80, IsAtiva = true }
        };

        _context.Disciplinas.AddRange(disciplinas);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var disciplina in disciplinas)
        {
            foreach (var serie in seriesDoFundamental2)
            {
                _context.DisciplinaSeries.Add(new Domain.Entities.DisciplinaSerie
                {
                    Id = Guid.NewGuid(),
                    DisciplinaId = disciplina.Id,
                    SerieId = serie.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProfessoresAsync(CancellationToken cancellationToken)
    {
        var disciplinaMatematica = await _context.Disciplinas.FirstOrDefaultAsync(x => x.Codigo == "MAT-01", cancellationToken);
        var disciplinaHistoria = await _context.Disciplinas.FirstOrDefaultAsync(x => x.Codigo == "HIS-01", cancellationToken);
        var turma8A = await _context.Turmas.FirstOrDefaultAsync(x => x.Nome == "8o Ano A", cancellationToken);
        var turma9B = await _context.Turmas.FirstOrDefaultAsync(x => x.Nome == "9o Ano B", cancellationToken);
        var turma7C = await _context.Turmas.FirstOrDefaultAsync(x => x.Nome == "7o Ano C", cancellationToken);

        if (disciplinaMatematica is null || disciplinaHistoria is null
            || turma8A is null || turma9B is null || turma7C is null)
        {
            return;
        }

        var juliana = await _context.Professores.FirstOrDefaultAsync(x => x.UsuarioCpf == "11122233344", cancellationToken);
        if (juliana is null)
        {
            juliana = new Domain.Entities.Professor
            {
                Id = Guid.NewGuid(),
                NomeCompleto = "Juliana Moraes",
                Email = "juliana.moraes@escola.local",
                UsuarioCpf = "11122233344",
                IsAtivo = true
            };

            _context.Professores.Add(juliana);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var ricardo = await _context.Professores.FirstOrDefaultAsync(x => x.UsuarioCpf == "55566677788", cancellationToken);
        if (ricardo is null)
        {
            ricardo = new Domain.Entities.Professor
            {
                Id = Guid.NewGuid(),
                NomeCompleto = "Ricardo Souza",
                Email = "ricardo.souza@escola.local",
                UsuarioCpf = "55566677788",
                IsAtivo = true
            };

            _context.Professores.Add(ricardo);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Garante os vínculos padrão de turma/disciplina para os professores seedados, mesmo que eles já
        // existissem de uma execução anterior (ex.: antes da tabela ProfessorAtribuicoes existir).
        if (!await _context.ProfessorAtribuicoes.AnyAsync(x => x.ProfessorId == juliana.Id, cancellationToken))
        {
            _context.ProfessorAtribuicoes.AddRange(
                new Domain.Entities.ProfessorAtribuicao { Id = Guid.NewGuid(), ProfessorId = juliana.Id, TurmaId = turma8A.Id, DisciplinaId = disciplinaMatematica.Id },
                new Domain.Entities.ProfessorAtribuicao { Id = Guid.NewGuid(), ProfessorId = juliana.Id, TurmaId = turma9B.Id, DisciplinaId = disciplinaMatematica.Id });
        }

        if (!await _context.ProfessorAtribuicoes.AnyAsync(x => x.ProfessorId == ricardo.Id, cancellationToken))
        {
            _context.ProfessorAtribuicoes.AddRange(
                new Domain.Entities.ProfessorAtribuicao { Id = Guid.NewGuid(), ProfessorId = ricardo.Id, TurmaId = turma7C.Id, DisciplinaId = disciplinaHistoria.Id },
                new Domain.Entities.ProfessorAtribuicao { Id = Guid.NewGuid(), ProfessorId = ricardo.Id, TurmaId = turma8A.Id, DisciplinaId = disciplinaHistoria.Id });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Garante conta de acesso para professores que ainda não têm uma (ex.: cadastrados antes desta
        // funcionalidade existir). Nunca mexe em conta já existente, para não resetar a senha de quem já a trocou.
        var professoresAtivos = await _context.Professores
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var professor in professoresAtivos)
        {
            if (await _userManager.FindByNameAsync(professor.UsuarioCpf) is not null)
            {
                continue;
            }

            await _professorAccountProvisioner.CriarOuReiniciarContaAsync(
                professor.UsuarioCpf, professor.NomeCompleto, professor.Email, "Professor@123", "Professor", cancellationToken);
        }
    }

    private async Task SeedTurmasAsync(CancellationToken cancellationToken)
    {
        if (await _context.Turmas.AnyAsync(cancellationToken))
        {
            return;
        }

        var serie6Ano = await _context.Series.FirstOrDefaultAsync(x => x.Nome == "6o Ano", cancellationToken);
        var serie7Ano = await _context.Series.FirstOrDefaultAsync(x => x.Nome == "7o Ano", cancellationToken);
        var serie8Ano = await _context.Series.FirstOrDefaultAsync(x => x.Nome == "8o Ano", cancellationToken);
        var serie9Ano = await _context.Series.FirstOrDefaultAsync(x => x.Nome == "9o Ano", cancellationToken);

        if (serie6Ano is null || serie7Ano is null || serie8Ano is null || serie9Ano is null)
        {
            return;
        }

        var turmas = new[]
        {
            new Domain.Entities.Turma
            {
                Id = Guid.NewGuid(),
                Nome = "6o Ano A",
                SerieId = serie6Ano.Id,
                Turno = "Manhã",
                AnoLetivo = 2026,
                IsAtiva = true
            },
            new Domain.Entities.Turma
            {
                Id = Guid.NewGuid(),
                Nome = "7o Ano C",
                SerieId = serie7Ano.Id,
                Turno = "Manhã",
                AnoLetivo = 2026,
                IsAtiva = true
            },
            new Domain.Entities.Turma
            {
                Id = Guid.NewGuid(),
                Nome = "8o Ano A",
                SerieId = serie8Ano.Id,
                Turno = "Tarde",
                AnoLetivo = 2026,
                IsAtiva = true
            },
            new Domain.Entities.Turma
            {
                Id = Guid.NewGuid(),
                Nome = "9o Ano B",
                SerieId = serie9Ano.Id,
                Turno = "Manhã",
                AnoLetivo = 2026,
                IsAtiva = true
            }
        };

        _context.Turmas.AddRange(turmas);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPeriodosAsync(CancellationToken cancellationToken)
    {
        if (await _context.PeriodosLancamento.AnyAsync(cancellationToken))
        {
            return;
        }

        var periodos = new[]
        {
            new Domain.Entities.PeriodoLancamento
            {
                Id = Guid.NewGuid(),
                AnoLetivo = 2026,
                Bimestre = 1,
                Descricao = "1o Trimestre 2026",
                DataInicial = new DateTime(2026, 2, 1),
                DataFinal = new DateTime(2026, 5, 31),
                IsAberto = false
            },
            new Domain.Entities.PeriodoLancamento
            {
                Id = Guid.NewGuid(),
                AnoLetivo = 2026,
                Bimestre = 2,
                Descricao = "2o Trimestre 2026",
                DataInicial = new DateTime(2026, 6, 1),
                DataFinal = new DateTime(2026, 8, 31),
                IsAberto = true
            },
            new Domain.Entities.PeriodoLancamento
            {
                Id = Guid.NewGuid(),
                AnoLetivo = 2026,
                Bimestre = 3,
                Descricao = "3o Trimestre 2026",
                DataInicial = new DateTime(2026, 9, 1),
                DataFinal = new DateTime(2026, 12, 20),
                IsAberto = false
            }
        };

        _context.PeriodosLancamento.AddRange(periodos);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedNotasAsync(CancellationToken cancellationToken)
    {
        if (await _context.Notas.AnyAsync(cancellationToken))
        {
            return;
        }

        var alunoGabriel = await _context.Alunos.FirstOrDefaultAsync(x => !x.IsDeleted && x.NomeCompleto == "Gabriel Santos", cancellationToken);
        var alunoAna = await _context.Alunos.FirstOrDefaultAsync(x => !x.IsDeleted && x.NomeCompleto == "Ana Beatriz Lima", cancellationToken);
        var disciplinaMatematica = await _context.Disciplinas.FirstOrDefaultAsync(x => !x.IsDeleted && x.Codigo == "MAT-01", cancellationToken);
        var disciplinaHistoria = await _context.Disciplinas.FirstOrDefaultAsync(x => !x.IsDeleted && x.Codigo == "HIS-01", cancellationToken);
        var professoraJuliana = await _context.Professores.FirstOrDefaultAsync(x => !x.IsDeleted && x.NomeCompleto == "Juliana Moraes", cancellationToken);
        var professorRicardo = await _context.Professores.FirstOrDefaultAsync(x => !x.IsDeleted && x.NomeCompleto == "Ricardo Souza", cancellationToken);
        var periodo2Trimestre = await _context.PeriodosLancamento.FirstOrDefaultAsync(x => !x.IsDeleted && x.AnoLetivo == 2026 && x.Bimestre == 2, cancellationToken);

        if (alunoGabriel is null || alunoAna is null || disciplinaMatematica is null || disciplinaHistoria is null
            || professoraJuliana is null || professorRicardo is null || periodo2Trimestre is null)
        {
            return;
        }

        var notas = new[]
        {
            new Domain.Entities.Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoGabriel.Id,
                DisciplinaId = disciplinaMatematica.Id,
                ProfessorId = professoraJuliana.Id,
                PeriodoLancamentoId = periodo2Trimestre.Id,
                Avaliacao1 = 8.0m,
                Avaliacao2 = 8.5m,
                Avaliacao3 = 9.0m,
                RecuperacaoParalela = null,
                ResultadoUnidade = 8.5m,
                ResultadoFinalUnidade = 8.5m,
                Valor = 8.5m,
                IsFinalizada = true
            },
            new Domain.Entities.Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoAna.Id,
                DisciplinaId = disciplinaHistoria.Id,
                ProfessorId = professorRicardo.Id,
                PeriodoLancamentoId = periodo2Trimestre.Id,
                Avaliacao1 = 3.5m,
                Avaliacao2 = 4.0m,
                Avaliacao3 = 4.5m,
                RecuperacaoParalela = 6.0m,
                ResultadoUnidade = 4.0m,
                ResultadoFinalUnidade = 6.0m,
                Valor = 6.0m,
                IsFinalizada = false
            }
        };

        _context.Notas.AddRange(notas);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDirectorUserAsync()
    {
        var cpf = NormalizeCpf(_configuration["SeedUser:Diretor:Cpf"]);
        var password = _configuration["SeedUser:Diretor:Password"];
        var name = _configuration["SeedUser:Diretor:FullName"] ?? "Diretor Padrão";
        var email = _configuration["SeedUser:Diretor:Email"];

        if (string.IsNullOrWhiteSpace(cpf) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existingUser = await _userManager.FindByNameAsync(cpf);
        if (existingUser is not null)
        {
            if (!await _userManager.IsInRoleAsync(existingUser, "Diretor"))
            {
                await _userManager.AddToRoleAsync(existingUser, "Diretor");
            }

            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = cpf,
            Email = email,
            FullName = name,
            IsActive = true,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return;
        }

        await _userManager.AddToRoleAsync(user, "Diretor");
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
