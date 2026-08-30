using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEscolar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProfessorAtribuicaoVinculoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Disciplinas",
                table: "Professores");

            migrationBuilder.DropColumn(
                name: "Series",
                table: "Professores");

            migrationBuilder.DropColumn(
                name: "Turmas",
                table: "Professores");

            migrationBuilder.CreateTable(
                name: "ProfessorAtribuicoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisciplinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessorAtribuicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessorAtribuicoes_Disciplinas_DisciplinaId",
                        column: x => x.DisciplinaId,
                        principalTable: "Disciplinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfessorAtribuicoes_Professores_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Professores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfessorAtribuicoes_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorAtribuicoes_DisciplinaId",
                table: "ProfessorAtribuicoes",
                column: "DisciplinaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorAtribuicoes_ProfessorId_TurmaId_DisciplinaId",
                table: "ProfessorAtribuicoes",
                columns: new[] { "ProfessorId", "TurmaId", "DisciplinaId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorAtribuicoes_TurmaId",
                table: "ProfessorAtribuicoes",
                column: "TurmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfessorAtribuicoes");

            migrationBuilder.AddColumn<string>(
                name: "Disciplinas",
                table: "Professores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Series",
                table: "Professores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Turmas",
                table: "Professores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
