using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Navigation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "navigation");

            migrationBuilder.CreateTable(
                name: "menus",
                schema: "navigation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_menus_menus_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "navigation",
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_permissions",
                schema: "navigation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_permissions_menus_menu_id",
                        column: x => x.menu_id,
                        principalSchema: "navigation",
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_menu_permissions_menu_id_permission_code",
                schema: "navigation",
                table: "menu_permissions",
                columns: new[] { "menu_id", "permission_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menus_code",
                schema: "navigation",
                table: "menus",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menus_parent_id",
                schema: "navigation",
                table: "menus",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_permissions",
                schema: "navigation");

            migrationBuilder.DropTable(
                name: "menus",
                schema: "navigation");
        }
    }
}
