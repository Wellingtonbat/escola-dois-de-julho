using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Auditoria;
using SistemaEscolar.Domain.Entities;
using SistemaEscolar.Infrastructure.Identity;
using SistemaEscolar.Infrastructure.Persistence;
using SistemaEscolar.Infrastructure.Persistence.Interceptors;
using SistemaEscolar.Tests.Support;

namespace SistemaEscolar.Tests;

public sealed class AuditoriaTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private sealed class UsuarioLogado : ICurrentUserService
    {
        public string? UserId => UsuarioId.ToString();
        public string? UserName => "02545277525";
        public string? FullName => "Maria Diretora";
        public bool IsInRole(string role) => false;
    }

    private static async Task<(SqliteConnection Conexao, ApplicationDbContext Contexto)> CriarContextoAsync()
    {
        var conexao = new SqliteConnection("DataSource=:memory:");
        await conexao.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conexao)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(new UsuarioLogado()))
            .Options;

        var contexto = new ApplicationDbContext(options);
        await contexto.Database.EnsureCreatedAsync();
        return (conexao, contexto);
    }

    // ----- Gravação (interceptor) -----

    [Fact]
    public async Task Interceptor_RegistraQuemFezACriacaoEAlteracao()
    {
        var (conexao, contexto) = await CriarContextoAsync();
        await using var _ = conexao;
        await using var __ = contexto;

        var serie = new Serie { Id = Guid.NewGuid(), Nome = "6º Ano", Ordem = 6, IsAtiva = true };
        contexto.Add(serie);
        await contexto.SaveChangesAsync();

        serie.Nome = "7º Ano";
        await contexto.SaveChangesAsync();

        var registros = await contexto.AuditLogs.Where(x => x.TableName == "Series").OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, registros.Count);
        Assert.All(registros, r =>
        {
            Assert.Equal(UsuarioId.ToString(), r.CreatedBy);
            Assert.Equal("02545277525", r.UserName);
            Assert.Equal("Maria Diretora", r.UserFullName);
        });
        Assert.Equal("ADDED", registros[0].Action);
        Assert.Equal("MODIFIED", registros[1].Action);
        Assert.Equal("6º Ano", System.Text.Json.JsonDocument.Parse(registros[1].OldValues!).RootElement.GetProperty("Nome").GetString());
        Assert.Equal("7º Ano", System.Text.Json.JsonDocument.Parse(registros[1].NewValues!).RootElement.GetProperty("Nome").GetString());
    }

    [Fact]
    public async Task Interceptor_IgnoraGravacaoSemAlteracaoReal()
    {
        var (conexao, contexto) = await CriarContextoAsync();
        await using var _ = conexao;
        await using var __ = contexto;

        var serie = new Serie { Id = Guid.NewGuid(), Nome = "6º Ano", Ordem = 6, IsAtiva = true };
        contexto.Add(serie);
        await contexto.SaveChangesAsync();

        // Update() marca tudo como modificado mesmo com os mesmos valores (é o que os repositórios fazem).
        contexto.Update(serie);
        await contexto.SaveChangesAsync();

        Assert.Equal(1, await contexto.AuditLogs.CountAsync(x => x.TableName == "Series"));
    }

    [Fact]
    public async Task Interceptor_NuncaGravaSenhaNemCarimboDeSeguranca_ENaoRegistraRuidoDeLogin()
    {
        var (conexao, contexto) = await CriarContextoAsync();
        await using var _ = conexao;
        await using var __ = contexto;

        var usuario = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "12345678900",
            NormalizedUserName = "12345678900",
            FullName = "Fulano",
            PasswordHash = "HASH-SECRETO-1",
            SecurityStamp = "STAMP-1",
            ConcurrencyStamp = "CONC-1",
        };
        contexto.Add(usuario);
        await contexto.SaveChangesAsync();

        // Login com falha/sucesso só mexe em contadores e carimbos: não deve gerar registro.
        usuario.AccessFailedCount = 3;
        usuario.SecurityStamp = "STAMP-2";
        usuario.ConcurrencyStamp = "CONC-2";
        await contexto.SaveChangesAsync();
        Assert.Equal(1, await contexto.AuditLogs.CountAsync(x => x.TableName == "AspNetUsers"));

        // Troca de senha: registra que houve troca, sem revelar hash algum.
        usuario.PasswordHash = "HASH-SECRETO-2";
        await contexto.SaveChangesAsync();

        var registros = await contexto.AuditLogs.Where(x => x.TableName == "AspNetUsers").OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, registros.Count);

        var tudo = string.Join(' ', registros.SelectMany(r => new[] { r.OldValues, r.NewValues }));
        Assert.DoesNotContain("HASH-SECRETO", tudo);
        Assert.DoesNotContain("STAMP", tudo);
        Assert.DoesNotContain("CONC-", tudo);
        Assert.Contains(AuditableEntitySaveChangesInterceptor.ValorAlterado, registros[1].NewValues);
    }

    // ----- Leitura (serviço) -----

    private sealed class RepositorioFalso : IAuditoriaRepository
    {
        public List<AuditLog> Registros { get; } = new();
        public Dictionary<Guid, string> Nomes { get; } = new();
        public int Consultas { get; private set; }

        public Task<(IReadOnlyList<AuditLog> Itens, int Total)> ListarAsync(
            DateTime? deUtc, DateTime? ateUtcExclusivo, string? usuario, string? tabela, string? acao, int pular, int tomar,
            CancellationToken cancellationToken = default)
        {
            Consultas++;
            return Task.FromResult<(IReadOnlyList<AuditLog>, int)>((Registros, Registros.Count));
        }

        public Task<IReadOnlyDictionary<Guid, string>> ObterNomesReferenciasAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(Nomes);
    }

    private static AuditoriaFiltro Filtro() => new(null, null, null, null, null, 1, 50);

    [Theory]
    [InlineData(Perfis.Coordenador)]
    [InlineData(Perfis.Secretaria)]
    [InlineData(Perfis.Professor)]
    public async Task Servico_RecusaQuemNaoEDiretoria(string perfil)
    {
        var repositorio = new RepositorioFalso();
        var servico = new AuditoriaService(repositorio, new FakeCurrentUserService(null, perfil));

        var resultado = await servico.ListarAsync(Filtro());

        Assert.False(resultado.Succeeded);
        Assert.Equal(0, repositorio.Consultas);
    }

    [Theory]
    [InlineData(Perfis.Diretor)]
    [InlineData(Perfis.ViceDiretor)]
    public async Task Servico_MostraApenasOQueMudouComNomesLegiveis(string perfil)
    {
        var alunoId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        var periodoId = Guid.NewGuid();

        var repositorio = new RepositorioFalso();
        repositorio.Nomes[alunoId] = "Joana Aluna";
        repositorio.Nomes[disciplinaId] = "Matemática";
        repositorio.Nomes[periodoId] = "1º Trimestre 2026";
        repositorio.Nomes[UsuarioId] = "Maria Diretora";

        var chaves = $"{{\"Id\":\"{Guid.NewGuid()}\"}}";
        string Valores(string av1, string atualizadoEm) =>
            $"{{\"AlunoId\":\"{alunoId}\",\"DisciplinaId\":\"{disciplinaId}\",\"PeriodoLancamentoId\":\"{periodoId}\"," +
            $"\"Avaliacao1\":{av1},\"IsFinalizada\":false,\"IsDeleted\":false,\"UpdatedAtUtc\":\"{atualizadoEm}\"}}";

        repositorio.Registros.Add(new AuditLog
        {
            Id = 1,
            TableName = "Notas",
            Action = "MODIFIED",
            KeyValues = chaves,
            OldValues = Valores("5.5", "2026-09-01T10:00:00Z"),
            NewValues = Valores("7.25", "2026-09-24T18:30:00Z"),
            CreatedAtUtc = new DateTime(2026, 9, 24, 18, 30, 0, DateTimeKind.Utc),
            CreatedBy = UsuarioId.ToString(),
            UserName = "02545277525",
        });

        var servico = new AuditoriaService(repositorio, new FakeCurrentUserService(null, perfil));
        var resultado = await servico.ListarAsync(Filtro());

        Assert.True(resultado.Succeeded);
        var item = Assert.Single(resultado.Pagina!.Itens);

        Assert.Equal("Maria Diretora", item.Usuario);
        Assert.Equal("Alterado", item.Acao);
        Assert.Equal("Notas", item.Entidade);
        Assert.Equal("Joana Aluna • Matemática • 1º Trimestre 2026", item.Registro);
        Assert.Equal(new DateTime(2026, 9, 24, 15, 30, 0), item.DataHoraLocal); // UTC-3

        var alteracao = Assert.Single(item.Alteracoes);
        Assert.Equal("1ª avaliação", alteracao.Campo);
        Assert.Equal("5,5", alteracao.ValorAnterior);
        Assert.Equal("7,25", alteracao.ValorNovo);
    }

    [Fact]
    public async Task Servico_ClassificaExclusaoLogicaEIdentificaAcaoSemUsuario()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Registros.Add(new AuditLog
        {
            Id = 2,
            TableName = "Alunos",
            Action = "MODIFIED",
            OldValues = "{\"NomeCompleto\":\"Aluno X\",\"IsDeleted\":false}",
            NewValues = "{\"NomeCompleto\":\"Aluno X\",\"IsDeleted\":true}",
            CreatedAtUtc = DateTime.UtcNow,
        });
        repositorio.Registros.Add(new AuditLog
        {
            Id = 3,
            TableName = "Series",
            Action = "ADDED",
            NewValues = "{\"Nome\":\"9º Ano\",\"IsDeleted\":false}",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid().ToString(),
            UserName = "11122233344",
        });

        var servico = new AuditoriaService(repositorio, new FakeCurrentUserService(null, Perfis.Diretor));
        var itens = (await servico.ListarAsync(Filtro())).Pagina!.Itens;

        var exclusao = itens.Single(x => x.Id == 2);
        Assert.Equal("Excluído", exclusao.Acao);
        Assert.Equal("Sistema / não identificado", exclusao.Usuario);
        Assert.Equal("Aluno X", exclusao.Registro);

        var criacao = itens.Single(x => x.Id == 3);
        Assert.Equal("Criado", criacao.Acao);
        Assert.Equal("CPF 11122233344", criacao.Usuario);
        Assert.Contains(criacao.Alteracoes, a => a.Campo == "Nome" && a.ValorNovo == "9º Ano");
        Assert.DoesNotContain(criacao.Alteracoes, a => a.Campo == "Excluído");
    }
}
