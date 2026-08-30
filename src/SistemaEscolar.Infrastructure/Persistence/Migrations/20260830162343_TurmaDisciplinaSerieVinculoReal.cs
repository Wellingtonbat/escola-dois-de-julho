using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEscolar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TurmaDisciplinaSerieVinculoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Turmas: adiciona a FK real SerieId (ainda opcional) e migra os dados existentes,
            //    casando o texto solto antigo com a Série atual por nome normalizado (ignora
            //    maiúsculas/minúsculas, espaços e a troca "o" <-> "°" causada por renomeações).
            migrationBuilder.AddColumn<Guid>(
                name: "SerieId",
                table: "Turmas",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Turmas"" t
                SET ""SerieId"" = s.""Id""
                FROM ""Series"" s
                WHERE lower(replace(replace(t.""Serie"", '°', 'o'), ' ', '')) = lower(replace(replace(s.""Nome"", '°', 'o'), ' ', ''));
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "SerieId",
                table: "Turmas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Turmas_Serie",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "Serie",
                table: "Turmas");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_SerieId",
                table: "Turmas",
                column: "SerieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turmas_Series_SerieId",
                table: "Turmas",
                column: "SerieId",
                principalTable: "Series",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 2) Disciplinas: cria a tabela de vínculo real DisciplinaSeries e migra os dados existentes
            //    (o campo antigo "Series" era uma lista de nomes separada por vírgula) antes de remover
            //    a coluna de texto solto.
            migrationBuilder.CreateTable(
                name: "DisciplinaSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisciplinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerieId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinaSeries_Disciplinas_DisciplinaId",
                        column: x => x.DisciplinaId,
                        principalTable: "Disciplinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplinaSeries_Series_SerieId",
                        column: x => x.SerieId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaSeries_DisciplinaId_SerieId",
                table: "DisciplinaSeries",
                columns: new[] { "DisciplinaId", "SerieId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaSeries_SerieId",
                table: "DisciplinaSeries",
                column: "SerieId");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    d RECORD;
                    serie_name TEXT;
                    matched_serie_id UUID;
                BEGIN
                    FOR d IN SELECT ""Id"", ""Series"" FROM ""Disciplinas"" WHERE ""Series"" IS NOT NULL AND ""Series"" <> '' LOOP
                        FOREACH serie_name IN ARRAY string_to_array(d.""Series"", ',') LOOP
                            SELECT ""Id"" INTO matched_serie_id FROM ""Series""
                            WHERE lower(replace(replace(trim(serie_name), '°', 'o'), ' ', '')) = lower(replace(replace(""Nome"", '°', 'o'), ' ', ''))
                            LIMIT 1;

                            IF matched_serie_id IS NOT NULL THEN
                                INSERT INTO ""DisciplinaSeries"" (""Id"", ""DisciplinaId"", ""SerieId"", ""CreatedAtUtc"", ""IsDeleted"")
                                VALUES (gen_random_uuid(), d.""Id"", matched_serie_id, now(), false)
                                ON CONFLICT (""DisciplinaId"", ""SerieId"") WHERE ""IsDeleted"" = false DO NOTHING;
                            END IF;
                        END LOOP;
                    END LOOP;
                END $$;
            ");

            migrationBuilder.DropColumn(
                name: "Series",
                table: "Disciplinas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turmas_Series_SerieId",
                table: "Turmas");

            migrationBuilder.DropTable(
                name: "DisciplinaSeries");

            migrationBuilder.DropIndex(
                name: "IX_Turmas_SerieId",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "SerieId",
                table: "Turmas");

            migrationBuilder.AddColumn<string>(
                name: "Serie",
                table: "Turmas",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Series",
                table: "Disciplinas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_Serie",
                table: "Turmas",
                column: "Serie");
        }
    }
}
