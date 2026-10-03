using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Seo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeoSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seo_schemas",
                schema: "seo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: true),
                    custom_json_ld = table.Column<string>(type: "text", nullable: true),
                    is_custom_override = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seo_schemas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_seo_schemas_entity_type_entity_id_schema_type",
                schema: "seo",
                table: "seo_schemas",
                columns: new[] { "entity_type", "entity_id", "schema_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seo_schemas_entity_type_schema_type_homepage",
                schema: "seo",
                table: "seo_schemas",
                columns: new[] { "entity_type", "schema_type" },
                unique: true,
                filter: "entity_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seo_schemas",
                schema: "seo");
        }
    }
}
