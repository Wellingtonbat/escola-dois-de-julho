using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEscolar.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Limpeza do histórico de auditoria gravado antes da tela de Auditoria:
    /// 1) esconde (IsDeleted = true, sem apagar nada) os registros que são só ruído: tabelas de controle do Identity
    ///    e gravações em que nenhum campo mudou de verdade (contadores de login, carimbos, campos técnicos);
    /// 2) mascara os hashes de senha e os carimbos de segurança que o interceptor antigo copiava para o histórico.
    /// Não há como restaurar os hashes (é o objetivo), por isso o Down não desfaz a etapa 2.
    /// </summary>
    public partial class AuditoriaLimpezaHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1a) Tabelas do Identity que só geram ruído.
            migrationBuilder.Sql(@"
UPDATE ""AuditLogs"" SET ""IsDeleted"" = TRUE
WHERE ""IsDeleted"" = FALSE
  AND ""TableName"" IN ('AspNetUserTokens', 'AspNetUserLogins', 'AspNetUserClaims', 'AspNetRoleClaims', 'AspNetRoles');
");

            // 1b) Alterações sem mudança real. Em bloco protegido: se algum registro antigo tiver JSON inválido,
            //     a limpeza é apenas pulada em vez de impedir o sistema de iniciar.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    UPDATE ""AuditLogs"" SET ""IsDeleted"" = TRUE
    WHERE ""IsDeleted"" = FALSE
      AND ""TableName"" = 'AspNetUsers' AND ""Action"" = 'MODIFIED'
      AND ""OldValues"" IS NOT NULL AND ""NewValues"" IS NOT NULL
      AND (""OldValues""::jsonb - 'ConcurrencyStamp' - 'SecurityStamp' - 'AccessFailedCount' - 'LockoutEnd' - 'LockoutEnabled' - 'UpdatedAtUtc' - 'UpdatedBy')
        = (""NewValues""::jsonb - 'ConcurrencyStamp' - 'SecurityStamp' - 'AccessFailedCount' - 'LockoutEnd' - 'LockoutEnabled' - 'UpdatedAtUtc' - 'UpdatedBy');

    UPDATE ""AuditLogs"" SET ""IsDeleted"" = TRUE
    WHERE ""IsDeleted"" = FALSE
      AND ""TableName"" <> 'AspNetUsers' AND ""Action"" = 'MODIFIED'
      AND ""OldValues"" IS NOT NULL AND ""NewValues"" IS NOT NULL
      AND (""OldValues""::jsonb - 'UpdatedAtUtc' - 'UpdatedBy') = (""NewValues""::jsonb - 'UpdatedAtUtc' - 'UpdatedBy');
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Limpeza de ruido da auditoria ignorada: %', SQLERRM;
END
$$;
");

            // 2a) Trocas de senha: o registro passa a dizer só que a senha foi alterada.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    UPDATE ""AuditLogs""
    SET ""NewValues"" = regexp_replace(""NewValues"", '""PasswordHash"":""[^""]*""', '""PasswordHash"":""[alterado]""')
    WHERE ""TableName"" = 'AspNetUsers' AND ""Action"" = 'MODIFIED'
      AND ""OldValues"" IS NOT NULL AND ""NewValues"" IS NOT NULL
      AND (""OldValues""::jsonb ->> 'PasswordHash') IS DISTINCT FROM (""NewValues""::jsonb ->> 'PasswordHash');
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Marcacao de troca de senha ignorada: %', SQLERRM;
END
$$;
");

            // 2b) Qualquer hash de senha que ainda esteja no histórico é mascarado.
            migrationBuilder.Sql(@"
UPDATE ""AuditLogs""
SET ""OldValues"" = regexp_replace(""OldValues"", '""PasswordHash"":""(?!\[)[^""]*""', '""PasswordHash"":""[oculto]""', 'g'),
    ""NewValues"" = regexp_replace(""NewValues"", '""PasswordHash"":""(?!\[)[^""]*""', '""PasswordHash"":""[oculto]""', 'g')
WHERE ""TableName"" = 'AspNetUsers'
  AND (""OldValues"" LIKE '%""PasswordHash"":""%' OR ""NewValues"" LIKE '%""PasswordHash"":""%');
");

            // 2c) Carimbos de segurança (usados para validar sessões) também saem do histórico.
            migrationBuilder.Sql(@"
UPDATE ""AuditLogs""
SET ""OldValues"" = regexp_replace(""OldValues"", '""SecurityStamp"":""[^""]*""', '""SecurityStamp"":""[oculto]""', 'g'),
    ""NewValues"" = regexp_replace(""NewValues"", '""SecurityStamp"":""[^""]*""', '""SecurityStamp"":""[oculto]""', 'g')
WHERE ""TableName"" = 'AspNetUsers'
  AND (""OldValues"" LIKE '%""SecurityStamp"":""%' OR ""NewValues"" LIKE '%""SecurityStamp"":""%');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Os hashes mascarados não podem (nem devem) ser restaurados. Reexibe apenas os registros de ruído.
            migrationBuilder.Sql(@"UPDATE ""AuditLogs"" SET ""IsDeleted"" = FALSE WHERE ""IsDeleted"" = TRUE;");
        }
    }
}
