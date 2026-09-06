using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEscolar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlunoTurmaVinculoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona a FK real TurmaId (opcional) e migra os dados existentes, casando o texto solto
            // antigo com a Turma atual por nome normalizado (ignora maiúsculas/minúsculas, espaços e a
            // troca "o" <-> "°" causada por renomeações), exigindo também que a turma pertença à mesma
            // série do aluno para evitar colisões de nomes entre séries diferentes.
            migrationBuilder.AddColumn<Guid>(
                name: "TurmaId",
                table: "Alunos",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Alunos"" a
                SET ""TurmaId"" = t.""Id""
                FROM ""Turmas"" t
                WHERE a.""Turma"" IS NOT NULL
                  AND t.""SerieId"" = a.""SerieId""
                  AND lower(replace(replace(trim(a.""Turma""), '°', 'o'), ' ', '')) = lower(replace(replace(t.""Nome"", '°', 'o'), ' ', ''));
            ");

            migrationBuilder.DropColumn(
                name: "Turma",
                table: "Alunos");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_TurmaId",
                table: "Alunos",
                column: "TurmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alunos_Turmas_TurmaId",
                table: "Alunos",
                column: "TurmaId",
                principalTable: "Turmas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alunos_Turmas_TurmaId",
                table: "Alunos");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_TurmaId",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "TurmaId",
                table: "Alunos");

            migrationBuilder.AddColumn<string>(
                name: "Turma",
                table: "Alunos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }
    }
}
