using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Entities;
using SistemaEscolar.Infrastructure.Identity;

namespace SistemaEscolar.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<DisciplinaSerie> DisciplinaSeries => Set<DisciplinaSerie>();
    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<PeriodoLancamento> PeriodosLancamento => Set<PeriodoLancamento>();
    public DbSet<Professor> Professores => Set<Professor>();
    public DbSet<ProfessorAtribuicao> ProfessorAtribuicoes => Set<ProfessorAtribuicao>();
    public DbSet<Serie> Series => Set<Serie>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Gera a matrícula amigável de cada novo aluno (número curto e memorizável).
        // O mesmo CPF reaproveita a matrícula já emitida em anos letivos anteriores.
        // Sequences nativas não existem no SQLite (usado nos testes de integração), só no Postgres.
        if (Database.IsNpgsql())
        {
            builder.HasSequence<long>("AlunoMatriculaSequence").StartsAt(100001).IncrementsBy(1);
        }

        builder.Entity<Aluno>(entity =>
        {
            entity.ToTable("Alunos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Matricula).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Cpf).HasMaxLength(11).IsRequired();
            entity.Property(x => x.NomeCompleto).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DataNascimento).HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(x => x.AnoLetivo).IsRequired();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => x.Matricula);
            entity.HasIndex(x => new { x.Cpf, x.AnoLetivo }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);

            entity.HasOne<Serie>()
                .WithMany()
                .HasForeignKey(x => x.SerieId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Turma>()
                .WithMany()
                .HasForeignKey(x => x.TurmaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Professor>(entity =>
        {
            entity.ToTable("Professores");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NomeCompleto).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UsuarioCpf).HasMaxLength(14).IsRequired();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => new { x.NomeCompleto, x.Email }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(x => x.UsuarioCpf).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        });

        builder.Entity<ProfessorAtribuicao>(entity =>
        {
            entity.ToTable("ProfessorAtribuicoes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => new { x.ProfessorId, x.TurmaId, x.DisciplinaId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);

            entity.HasOne<Professor>()
                .WithMany()
                .HasForeignKey(x => x.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Turma>()
                .WithMany()
                .HasForeignKey(x => x.TurmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Disciplina>()
                .WithMany()
                .HasForeignKey(x => x.DisciplinaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Disciplina>(entity =>
        {
            entity.ToTable("Disciplinas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CargaHoraria).IsRequired();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => x.Codigo).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(x => x.Nome);
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        });

        builder.Entity<DisciplinaSerie>(entity =>
        {
            entity.ToTable("DisciplinaSeries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => new { x.DisciplinaId, x.SerieId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);

            entity.HasOne<Disciplina>().WithMany().HasForeignKey(x => x.DisciplinaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Serie>().WithMany().HasForeignKey(x => x.SerieId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Nota>(entity =>
        {
            entity.ToTable("Notas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Avaliacao1).HasPrecision(4, 2);
            entity.Property(x => x.Avaliacao2).HasPrecision(4, 2);
            entity.Property(x => x.Avaliacao3).HasPrecision(4, 2);
            entity.Property(x => x.RecuperacaoParalela).HasPrecision(4, 2);
            entity.Property(x => x.ResultadoUnidade).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.ResultadoFinalUnidade).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.Valor).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => new { x.AlunoId, x.DisciplinaId, x.PeriodoLancamentoId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(x => x.ProfessorId);
            entity.HasIndex(x => x.PeriodoLancamentoId);
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);

            entity.HasOne<Aluno>()
                .WithMany()
                .HasForeignKey(x => x.AlunoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Disciplina>()
                .WithMany()
                .HasForeignKey(x => x.DisciplinaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Professor>()
                .WithMany()
                .HasForeignKey(x => x.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PeriodoLancamento>()
                .WithMany()
                .HasForeignKey(x => x.PeriodoLancamentoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PeriodoLancamento>(entity =>
        {
            entity.ToTable("PeriodosLancamento");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AnoLetivo).IsRequired();
            entity.Property(x => x.Bimestre).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DataInicial).HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(x => x.DataFinal).HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => new { x.AnoLetivo, x.Bimestre }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(x => new { x.DataInicial, x.DataFinal });
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        });

        builder.Entity<Turma>(entity =>
        {
            entity.ToTable("Turmas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Turno).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AnoLetivo).IsRequired();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => new { x.Nome, x.AnoLetivo }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(x => x.SerieId);
            entity.HasIndex(x => x.AnoLetivo);
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);

            entity.HasOne<Serie>().WithMany().HasForeignKey(x => x.SerieId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Serie>(entity =>
        {
            entity.ToTable("Series");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Ordem).IsRequired();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => x.Nome).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TableName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(16).IsRequired();
            entity.Property(x => x.KeyValues).HasColumnType("TEXT");
            entity.Property(x => x.OldValues).HasColumnType("TEXT");
            entity.Property(x => x.NewValues).HasColumnType("TEXT");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        });
    }
}
