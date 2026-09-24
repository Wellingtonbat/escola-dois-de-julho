using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Domain.Entities;
using SistemaEscolar.Tests.Support;

namespace SistemaEscolar.Tests;

// Garante no serviço (e não só nas telas) quem pode alterar notas: Diretor/Vice-Diretor em qualquer
// período, Professor só com o período aberto e no próprio escopo, Coordenador/Secretária só consultam.
public sealed class NotaServicePermissoesTests
{
    private static readonly Guid TurmaDoProfessor = Guid.NewGuid();
    private static readonly Guid TurmaDeOutroProfessor = Guid.NewGuid();
    private static readonly Guid DisciplinaDoProfessor = Guid.NewGuid();
    private static readonly Guid ProfessorLogadoId = Guid.NewGuid();
    private static readonly Guid PeriodoAbertoId = Guid.NewGuid();
    private static readonly Guid PeriodoFechadoId = Guid.NewGuid();

    private const string CpfProfessorLogado = "11111111111";
    private const string MensagemConsulta = "Seu perfil tem acesso somente de consulta às notas.";

    private static readonly Guid AlunoNoEscopo = Guid.NewGuid();
    private static readonly Guid AlunoForaDoEscopo = Guid.NewGuid();

    private sealed class Cenario
    {
        public required NotaService Service { get; init; }
        public required FakeNotaRepository Repositorio { get; init; }
    }

    private static Cenario Montar(params string[] perfis)
    {
        var repositorio = new FakeNotaRepository();
        var service = new NotaService(
            repositorio,
            new FakeAlunoService(),
            new FakeCurrentUserService(CpfProfessorLogado, perfis),
            new FakeDisciplinaService(),
            new FakePeriodoService(),
            new FakeProfessorService());

        return new Cenario { Service = service, Repositorio = repositorio };
    }

    private static NotaCreateRequest Lancamento(Guid alunoId, Guid periodoId) =>
        new(alunoId, DisciplinaDoProfessor, ProfessorLogadoId, periodoId, 4m, 3m, null, null, false);

    [Theory]
    [InlineData(Perfis.Coordenador)]
    [InlineData(Perfis.CoordenadorLegado)]
    [InlineData(Perfis.Secretaria)]
    public async Task CoordenadorESecretaria_NaoAlteramNotasNemComPeriodoAberto(string perfil)
    {
        var cenario = Montar(perfil);
        var existente = cenario.Repositorio.Adicionar(AlunoNoEscopo, PeriodoAbertoId);

        var criar = await cenario.Service.CriarAsync(Lancamento(AlunoForaDoEscopo, PeriodoAbertoId));
        var atualizar = await cenario.Service.AtualizarAsync(existente.Id, Lancamento(AlunoNoEscopo, PeriodoAbertoId));

        Assert.False(criar.Succeeded);
        Assert.Equal(MensagemConsulta, criar.ErrorMessage);
        Assert.False(atualizar.Succeeded);
        Assert.Equal(MensagemConsulta, atualizar.ErrorMessage);
        Assert.False(await cenario.Service.AlternarFinalizacaoAsync(existente.Id));
        Assert.False(await cenario.Service.ExcluirAsync(existente.Id));
        Assert.Single(cenario.Repositorio.Notas);
        Assert.False(existente.IsFinalizada);
        Assert.False(existente.IsDeleted);
    }

    [Fact]
    public async Task Professor_AlteraNoPeriodoAbertoDentroDoProprioEscopo()
    {
        var cenario = Montar(Perfis.Professor);

        var resultado = await cenario.Service.CriarAsync(Lancamento(AlunoNoEscopo, PeriodoAbertoId));

        Assert.True(resultado.Succeeded, resultado.ErrorMessage);
    }

    [Fact]
    public async Task Professor_NaoAlteraComPeriodoFechado()
    {
        var cenario = Montar(Perfis.Professor);
        var existente = cenario.Repositorio.Adicionar(AlunoNoEscopo, PeriodoFechadoId);

        var criar = await cenario.Service.CriarAsync(Lancamento(AlunoNoEscopo, PeriodoFechadoId));

        Assert.False(criar.Succeeded);
        Assert.False(await cenario.Service.AlternarFinalizacaoAsync(existente.Id));
        Assert.False(await cenario.Service.ExcluirAsync(existente.Id));
    }

    [Fact]
    public async Task Professor_NaoAlteraForaDoProprioEscopo()
    {
        var cenario = Montar(Perfis.Professor);

        var resultado = await cenario.Service.CriarAsync(Lancamento(AlunoForaDoEscopo, PeriodoAbertoId));

        Assert.False(resultado.Succeeded);
        Assert.Contains("vinculado", resultado.ErrorMessage);
    }

    [Theory]
    [InlineData(Perfis.Diretor)]
    [InlineData(Perfis.ViceDiretor)]
    public async Task Diretoria_AlteraQualquerNotaEmQualquerPeriodo(string perfil)
    {
        var cenario = Montar(perfil);
        var noFechado = cenario.Repositorio.Adicionar(AlunoForaDoEscopo, PeriodoFechadoId);

        var criarForaDoEscopoEmPeriodoFechado = await cenario.Service.CriarAsync(Lancamento(AlunoForaDoEscopo, PeriodoAbertoId));
        var atualizarPeriodoFechado = await cenario.Service.AtualizarAsync(noFechado.Id, Lancamento(AlunoForaDoEscopo, PeriodoFechadoId));

        Assert.True(criarForaDoEscopoEmPeriodoFechado.Succeeded, criarForaDoEscopoEmPeriodoFechado.ErrorMessage);
        Assert.True(atualizarPeriodoFechado.Succeeded, atualizarPeriodoFechado.ErrorMessage);
        Assert.True(await cenario.Service.AlternarFinalizacaoAsync(noFechado.Id));
        Assert.True(await cenario.Service.ExcluirAsync(noFechado.Id));
        Assert.True(noFechado.IsDeleted);
    }

    [Fact]
    public async Task ProfessorQueEViceDiretor_AlteraForaDoEscopoEForaDoPeriodo_ELeTodasAsNotas()
    {
        var cenario = Montar(Perfis.Professor, Perfis.ViceDiretor);
        cenario.Repositorio.Adicionar(AlunoNoEscopo, PeriodoAbertoId);
        cenario.Repositorio.Adicionar(AlunoForaDoEscopo, PeriodoFechadoId);

        var foraDoEscopo = await cenario.Service.CriarAsync(Lancamento(AlunoForaDoEscopo, PeriodoAbertoId));
        var listadas = await cenario.Service.ListarAsync();

        Assert.True(foraDoEscopo.Succeeded, foraDoEscopo.ErrorMessage);
        // Vê as notas de todos (o professor "puro" só enxergaria as do próprio escopo).
        Assert.Equal(3, listadas.Count);
    }

    [Fact]
    public async Task ProfessorPuro_SoListaAsProprias()
    {
        var cenario = Montar(Perfis.Professor);
        cenario.Repositorio.Adicionar(AlunoNoEscopo, PeriodoAbertoId, ProfessorLogadoId);
        cenario.Repositorio.Adicionar(AlunoForaDoEscopo, PeriodoAbertoId, Guid.NewGuid());

        var listadas = await cenario.Service.ListarAsync();

        Assert.Single(listadas);
    }

    [Fact]
    public async Task ProfessorQueECoordenador_ContinuaRestritoAoProprioEscopoParaAlterar()
    {
        var cenario = Montar(Perfis.Professor, Perfis.Coordenador);

        var noEscopo = await cenario.Service.CriarAsync(Lancamento(AlunoNoEscopo, PeriodoAbertoId));
        var foraDoEscopo = await cenario.Service.CriarAsync(Lancamento(AlunoForaDoEscopo, PeriodoAbertoId));

        Assert.True(noEscopo.Succeeded, noEscopo.ErrorMessage);
        Assert.False(foraDoEscopo.Succeeded);
    }

    // ----- Fakes -----

    private sealed class FakeNotaRepository : INotaRepository
    {
        public List<Nota> Notas { get; } = new();

        public Nota Adicionar(Guid alunoId, Guid periodoId, Guid? professorId = null)
        {
            var nota = new Nota
            {
                Id = Guid.NewGuid(),
                AlunoId = alunoId,
                DisciplinaId = DisciplinaDoProfessor,
                ProfessorId = professorId ?? ProfessorLogadoId,
                PeriodoLancamentoId = periodoId,
                Avaliacao1 = 5m,
                ResultadoUnidade = 5m,
                ResultadoFinalUnidade = 5m,
                Valor = 5m,
            };
            Notas.Add(nota);
            return nota;
        }

        public Task<IReadOnlyList<NotaListItemDto>> GetAllAsync(NotaListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<NotaListItemDto> itens = Notas
                .Where(x => !x.IsDeleted)
                .Select(x => new NotaListItemDto(
                    x.Id, x.AlunoId, "Aluno", x.DisciplinaId, "Disciplina", x.ProfessorId, "Professor",
                    x.PeriodoLancamentoId, "Período", 2026, 1, x.Avaliacao1, x.Avaliacao2, x.Avaliacao3,
                    x.RecuperacaoParalela, x.ResultadoUnidade, x.ResultadoFinalUnidade, x.IsFinalizada))
                .ToList();
            return Task.FromResult(itens);
        }

        public Task<Nota?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notas.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

        public Task<bool> LancamentoDuplicadoExisteAsync(Guid alunoId, Guid disciplinaId, Guid periodoLancamentoId, Guid? ignoreId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notas.Any(x => !x.IsDeleted && x.Id != ignoreId && x.AlunoId == alunoId
                && x.DisciplinaId == disciplinaId && x.PeriodoLancamentoId == periodoLancamentoId));

        public Task<bool> TemPendenciasAsync(Guid periodoLancamentoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Nota nota, CancellationToken cancellationToken = default)
        {
            Notas.Add(nota);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Nota nota, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SoftDeleteAsync(Nota nota, CancellationToken cancellationToken = default)
        {
            nota.IsDeleted = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAlunoService : IAlunoService
    {
        private static readonly IReadOnlyList<AlunoListItemDto> Alunos = new[]
        {
            new AlunoListItemDto(AlunoNoEscopo, "1", "1", "Aluno No Escopo", DateTime.Today, 2026, Guid.NewGuid(), "6º Ano", TurmaDoProfessor, "6A", true),
            new AlunoListItemDto(AlunoForaDoEscopo, "2", "2", "Aluno Fora Do Escopo", DateTime.Today, 2026, Guid.NewGuid(), "6º Ano", TurmaDeOutroProfessor, "6B", true),
        };

        public Task<IReadOnlyList<AlunoListItemDto>> ListarAsync(AlunoListFilter? filter = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Alunos);

        public Task<AlunoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Alunos.FirstOrDefault(x => x.Id == id));

        public Task<AlunoCreateResult> CriarAsync(AlunoCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AlunoCreateResult> AtualizarAsync(Guid id, AlunoCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeDisciplinaService : IDisciplinaService
    {
        private static readonly IReadOnlyList<DisciplinaListItemDto> Disciplinas = new[]
        {
            new DisciplinaListItemDto(DisciplinaDoProfessor, "Matemática", "MAT", Array.Empty<DisciplinaSerieDto>(), 80, true),
        };

        public Task<IReadOnlyList<DisciplinaListItemDto>> ListarAsync(DisciplinaListFilter? filter = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Disciplinas);

        public Task<DisciplinaListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DisciplinaCreateResult> CriarAsync(DisciplinaCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DisciplinaCreateResult> AtualizarAsync(Guid id, DisciplinaCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakePeriodoService : IPeriodoService
    {
        // Aberto por exceção manual (independe da data de hoje) e fechado.
        private static readonly IReadOnlyList<PeriodoListItemDto> Periodos = new[]
        {
            new PeriodoListItemDto(PeriodoAbertoId, 2026, 1, "1º Trimestre", DateTime.Today.AddDays(-90), DateTime.Today.AddDays(-30), true, true),
            new PeriodoListItemDto(PeriodoFechadoId, 2026, 2, "2º Trimestre", DateTime.Today.AddDays(-90), DateTime.Today.AddDays(-30), false, false),
        };

        public Task<IReadOnlyList<PeriodoListItemDto>> ListarAsync(PeriodoListFilter? filter = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Periodos);

        public Task<PeriodoListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Periodos.FirstOrDefault(x => x.Id == id));

        public Task<PeriodoListItemDto?> ObterPorAnoETrimestreAsync(int anoLetivo, int trimestre, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PeriodoCreateResult> CriarAsync(PeriodoCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PeriodoCreateResult> AtualizarAsync(Guid id, PeriodoCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PeriodoCreateResult> AbrirAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PeriodoCreateResult> FecharAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeProfessorService : IProfessorService
    {
        public Task<IReadOnlyList<ProfessorListItemDto>> ListarAsync(ProfessorListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProfessorListItemDto> professores = new[]
            {
                new ProfessorListItemDto(ProfessorLogadoId, "Professor Logado", "p@teste.local", CpfProfessorLogado, Array.Empty<ProfessorAtribuicaoDto>(), true),
            };
            return Task.FromResult(professores);
        }

        public Task<ProfessorEscopoDto?> ObterEscopoPorUsuarioAsync(string? userName, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProfessorEscopoDto?>(new ProfessorEscopoDto(
                ProfessorLogadoId,
                new[] { TurmaDoProfessor },
                new[] { DisciplinaDoProfessor },
                Array.Empty<Guid>()));

        public Task<ProfessorCreateResult> CriarAsync(ProfessorCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProfessorListItemDto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProfessorCreateResult> AtualizarAsync(Guid id, ProfessorCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AlternarStatusAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExcluirAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProfessorCreateResult> RedefinirSenhaAsync(Guid id, string novaSenha, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlySet<string>> ListarCpfsViceDiretoresAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
