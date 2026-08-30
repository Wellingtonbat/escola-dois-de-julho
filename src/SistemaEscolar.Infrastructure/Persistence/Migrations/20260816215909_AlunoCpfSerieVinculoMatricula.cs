using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEscolar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlunoCpfSerieVinculoMatricula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dados de desenvolvimento (fixtures) recriados automaticamente pelo seed a cada boot em ambiente
            // Development. Removidos aqui pois as novas colunas obrigatórias (Cpf, SerieId) não têm valor
            // válido para as linhas existentes.
            migrationBuilder.Sql("DELETE FROM \"Notas\";");
            migrationBuilder.Sql("DELETE FROM \"Alunos\";");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "Serie",
                table: "Alunos");

            migrationBuilder.CreateSequence(
                name: "AlunoMatriculaSequence",
                startValue: 100001L);

            migrationBuilder.AlterColumn<string>(
                name: "Turma",
                table: "Alunos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<int>(
                name: "AnoLetivo",
                table: "Alunos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Alunos",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SerieId",
                table: "Alunos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Cpf_AnoLetivo",
                table: "Alunos",
                columns: new[] { "Cpf", "AnoLetivo" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos",
                column: "Matricula");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_SerieId",
                table: "Alunos",
                column: "SerieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alunos_Series_SerieId",
                table: "Alunos",
                column: "SerieId",
                principalTable: "Series",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alunos_Series_SerieId",
                table: "Alunos");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_Cpf_AnoLetivo",
                table: "Alunos");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_SerieId",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "AnoLetivo",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "SerieId",
                table: "Alunos");

            migrationBuilder.DropSequence(
                name: "AlunoMatriculaSequence");

            migrationBuilder.AlterColumn<string>(
                name: "Turma",
                table: "Alunos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Serie",
                table: "Alunos",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos",
                column: "Matricula",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
