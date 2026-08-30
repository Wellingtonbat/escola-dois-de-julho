using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEscolar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotaPeriodoLancamentoVinculoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona a FK real PeriodoLancamentoId (ainda opcional) e migra os dados existentes,
            // casando o Ano/Bimestre soltos de cada nota com o período cadastrado correspondente.
            migrationBuilder.AddColumn<Guid>(
                name: "PeriodoLancamentoId",
                table: "Notas",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Notas"" n
                SET ""PeriodoLancamentoId"" = p.""Id""
                FROM ""PeriodosLancamento"" p
                WHERE n.""AnoLetivo"" = p.""AnoLetivo"" AND n.""Bimestre"" = p.""Bimestre"";
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "PeriodoLancamentoId",
                table: "Notas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_AnoLetivo_Bimestre",
                table: "Notas");

            migrationBuilder.DropColumn(
                name: "AnoLetivo",
                table: "Notas");

            migrationBuilder.DropColumn(
                name: "Bimestre",
                table: "Notas");

            migrationBuilder.CreateIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_PeriodoLancamentoId",
                table: "Notas",
                columns: new[] { "AlunoId", "DisciplinaId", "PeriodoLancamentoId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Notas_PeriodoLancamentoId",
                table: "Notas",
                column: "PeriodoLancamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notas_PeriodosLancamento_PeriodoLancamentoId",
                table: "Notas",
                column: "PeriodoLancamentoId",
                principalTable: "PeriodosLancamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notas_PeriodosLancamento_PeriodoLancamentoId",
                table: "Notas");

            migrationBuilder.DropIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_PeriodoLancamentoId",
                table: "Notas");

            migrationBuilder.DropIndex(
                name: "IX_Notas_PeriodoLancamentoId",
                table: "Notas");

            migrationBuilder.DropColumn(
                name: "PeriodoLancamentoId",
                table: "Notas");

            migrationBuilder.AddColumn<int>(
                name: "AnoLetivo",
                table: "Notas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Bimestre",
                table: "Notas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notas_AlunoId_DisciplinaId_AnoLetivo_Bimestre",
                table: "Notas",
                columns: new[] { "AlunoId", "DisciplinaId", "AnoLetivo", "Bimestre" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
