using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEscolar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartialUniqueIndexesSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turmas_Nome_AnoLetivo",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_Series_Nome",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Professores_NomeCompleto_Email",
                table: "Professores");

            migrationBuilder.DropIndex(
                name: "IX_Professores_UsuarioCpf",
                table: "Professores");

            migrationBuilder.DropIndex(
                name: "IX_PeriodosLancamento_AnoLetivo_Bimestre",
                table: "PeriodosLancamento");

            migrationBuilder.DropIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_AnoLetivo_Bimestre",
                table: "Notas");

            migrationBuilder.DropIndex(
                name: "IX_Disciplinas_Codigo",
                table: "Disciplinas");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_Nome_AnoLetivo",
                table: "Turmas",
                columns: new[] { "Nome", "AnoLetivo" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Series_Nome",
                table: "Series",
                column: "Nome",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Professores_NomeCompleto_Email",
                table: "Professores",
                columns: new[] { "NomeCompleto", "Email" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Professores_UsuarioCpf",
                table: "Professores",
                column: "UsuarioCpf",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosLancamento_AnoLetivo_Bimestre",
                table: "PeriodosLancamento",
                columns: new[] { "AnoLetivo", "Bimestre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_AnoLetivo_Bimestre",
                table: "Notas",
                columns: new[] { "AlunoId", "DisciplinaId", "AnoLetivo", "Bimestre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Disciplinas_Codigo",
                table: "Disciplinas",
                column: "Codigo",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos",
                column: "Matricula",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turmas_Nome_AnoLetivo",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_Series_Nome",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Professores_NomeCompleto_Email",
                table: "Professores");

            migrationBuilder.DropIndex(
                name: "IX_Professores_UsuarioCpf",
                table: "Professores");

            migrationBuilder.DropIndex(
                name: "IX_PeriodosLancamento_AnoLetivo_Bimestre",
                table: "PeriodosLancamento");

            migrationBuilder.DropIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_AnoLetivo_Bimestre",
                table: "Notas");

            migrationBuilder.DropIndex(
                name: "IX_Disciplinas_Codigo",
                table: "Disciplinas");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_Nome_AnoLetivo",
                table: "Turmas",
                columns: new[] { "Nome", "AnoLetivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_Nome",
                table: "Series",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Professores_NomeCompleto_Email",
                table: "Professores",
                columns: new[] { "NomeCompleto", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Professores_UsuarioCpf",
                table: "Professores",
                column: "UsuarioCpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosLancamento_AnoLetivo_Bimestre",
                table: "PeriodosLancamento",
                columns: new[] { "AnoLetivo", "Bimestre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_AnoLetivo_Bimestre",
                table: "Notas",
                columns: new[] { "AlunoId", "DisciplinaId", "AnoLetivo", "Bimestre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disciplinas_Codigo",
                table: "Disciplinas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos",
                column: "Matricula",
                unique: true);
        }
    }
}
