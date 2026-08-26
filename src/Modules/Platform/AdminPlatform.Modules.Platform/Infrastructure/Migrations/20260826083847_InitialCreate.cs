using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", nullable: true),
                    at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_years",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscal_years", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_user_id",
                schema: "platform",
                table: "audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_at_utc",
                schema: "platform",
                table: "audit_logs",
                column: "at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_name",
                schema: "platform",
                table: "audit_logs",
                column: "entity_name");

            migrationBuilder.CreateIndex(
                name: "ix_fiscal_years_organization_id_code",
                schema: "platform",
                table: "fiscal_years",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_organization_id_code",
                schema: "platform",
                table: "system_settings",
                columns: new[] { "organization_id", "code" },
                unique: true);

            // Cross-module references by id only (architecture assumption #6) — see the matching comment
            // in AccessControl's InitialCreate migration. system_settings.organization_id is nullable
            // (null = global setting), so this FK is not NOT NULL.
            migrationBuilder.Sql(
                "ALTER TABLE platform.fiscal_years " +
                "ADD CONSTRAINT fk_fiscal_years_organization_organizations_organization_id " +
                "FOREIGN KEY (organization_id) REFERENCES organization.organizations (id) ON DELETE RESTRICT;");
            migrationBuilder.Sql(
                "ALTER TABLE platform.system_settings " +
                "ADD CONSTRAINT fk_system_settings_organization_organizations_organization_id " +
                "FOREIGN KEY (organization_id) REFERENCES organization.organizations (id) ON DELETE RESTRICT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "fiscal_years",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "system_settings",
                schema: "platform");
        }
    }
}
