using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Navigation.Infrastructure.Migrations
{
    /// <summary>Turns the admin-only `menus` / `menu_permissions` tables into the shared navigation model
    /// (`navigation_menus` containers, `navigation_items` tree, `navigation_item_permissions`, plus
    /// `navigation_item_site_details` for website-only fields) WITHOUT losing data: the old tables are renamed, not
    /// dropped, and every existing row becomes an item of the new `admin-sidebar` container. EF's scaffold would have
    /// dropped and recreated them, so the Up/Down below are written by hand; the Designer file (target model) is as
    /// generated.</summary>
    public partial class UnifyNavigation : Migration
    {
        private const string AdminSidebarId = "5c1d6a50-0000-4000-8000-000000000001";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Containers, and the admin sidebar container that adopts every existing menu row.
            migrationBuilder.CreateTable(
                name: "navigation_menus",
                schema: "navigation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("pk_navigation_menus", x => x.id);
                    table.CheckConstraint("ck_navigation_menus_placement", "(scope = 'Admin' AND location = 'Sidebar') OR (scope = 'Site' AND location IN ('Header', 'Footer', 'Mobile'))");
                });

            migrationBuilder.CreateIndex(
                name: "ix_navigation_menus_code",
                schema: "navigation",
                table: "navigation_menus",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_navigation_menus_scope_location",
                schema: "navigation",
                table: "navigation_menus",
                columns: new[] { "scope", "location" },
                unique: true);

            migrationBuilder.Sql($@"
                INSERT INTO navigation.navigation_menus (id, scope, location, created_at_utc, code, name, is_active)
                VALUES ('{AdminSidebarId}', 'Admin', 'Sidebar', now(), 'admin-sidebar', 'Menu quản trị', true);");

            // 2. menus -> navigation_items (rename, keep every row).
            migrationBuilder.RenameTable(name: "menus", schema: "navigation", newName: "navigation_items", newSchema: "navigation");
            migrationBuilder.RenameColumn(name: "route", schema: "navigation", table: "navigation_items", newName: "url");

            migrationBuilder.AddColumn<Guid>(
                name: "menu_id", schema: "navigation", table: "navigation_items", type: "uuid", nullable: true);
            migrationBuilder.Sql($"UPDATE navigation.navigation_items SET menu_id = '{AdminSidebarId}';");
            migrationBuilder.AlterColumn<Guid>(
                name: "menu_id", schema: "navigation", table: "navigation_items", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_group", schema: "navigation", table: "navigation_items", type: "boolean", nullable: false, defaultValue: false);
            // An entry without a route was already a section heading.
            migrationBuilder.Sql("UPDATE navigation.navigation_items SET is_group = (url IS NULL);");

            // `code` was unique across the whole table; it is now unique per menu.
            migrationBuilder.DropIndex(name: "ix_menus_code", schema: "navigation", table: "navigation_items");

            // 3. menu_permissions -> navigation_item_permissions.
            migrationBuilder.RenameTable(name: "menu_permissions", schema: "navigation", newName: "navigation_item_permissions", newSchema: "navigation");
            migrationBuilder.RenameColumn(name: "menu_id", schema: "navigation", table: "navigation_item_permissions", newName: "item_id");

            // 4. Give the renamed objects the names the model (and every later migration) expects.
            RenameConstraint(migrationBuilder, "navigation_items", "pk_menus", "pk_navigation_items");
            RenameConstraint(migrationBuilder, "navigation_items", "fk_menus_menus_parent_id", "fk_navigation_items_navigation_items_parent_id");
            RenameIndex(migrationBuilder, "ix_menus_parent_id", "ix_navigation_items_parent_id");
            RenameConstraint(migrationBuilder, "navigation_item_permissions", "pk_menu_permissions", "pk_navigation_item_permissions");
            RenameConstraint(migrationBuilder, "navigation_item_permissions", "fk_menu_permissions_menus_menu_id", "fk_navigation_item_permissions_navigation_items_item_id");
            RenameIndex(migrationBuilder, "ix_menu_permissions_menu_id_permission_code", "ix_navigation_item_permissions_item_id_permission_code");

            // 5. New constraints and indexes on the shared item table.
            migrationBuilder.AddForeignKey(
                name: "fk_navigation_items_navigation_menus_menu_id",
                schema: "navigation",
                table: "navigation_items",
                column: "menu_id",
                principalSchema: "navigation",
                principalTable: "navigation_menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddCheckConstraint(
                name: "ck_navigation_items_group_has_no_url", schema: "navigation", table: "navigation_items",
                sql: "is_group = false OR url IS NULL");
            migrationBuilder.AddCheckConstraint(
                name: "ck_navigation_items_not_own_parent", schema: "navigation", table: "navigation_items",
                sql: "parent_id IS NULL OR parent_id <> id");

            migrationBuilder.CreateIndex(
                name: "ix_navigation_items_menu_id_code", schema: "navigation", table: "navigation_items",
                columns: new[] { "menu_id", "code" }, unique: true);
            migrationBuilder.CreateIndex(
                name: "ix_navigation_items_menu_id_parent_id_sort_order", schema: "navigation", table: "navigation_items",
                columns: new[] { "menu_id", "parent_id", "sort_order" });

            // 6. Website-only details (1 : 0..1 with an item).
            migrationBuilder.CreateTable(
                name: "navigation_item_site_details",
                schema: "navigation",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    open_in_new_tab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_navigation_item_site_details", x => x.item_id);
                    table.ForeignKey(
                        name: "fk_navigation_item_site_details_navigation_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "navigation",
                        principalTable: "navigation_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "navigation_item_site_details", schema: "navigation");

            // The old model only knows the admin sidebar: website items cannot be kept.
            migrationBuilder.Sql(@"
                UPDATE navigation.navigation_items SET parent_id = NULL
                WHERE menu_id IN (SELECT id FROM navigation.navigation_menus WHERE scope <> 'Admin');
                DELETE FROM navigation.navigation_item_permissions
                WHERE item_id IN (SELECT i.id FROM navigation.navigation_items i
                                  JOIN navigation.navigation_menus m ON m.id = i.menu_id WHERE m.scope <> 'Admin');
                DELETE FROM navigation.navigation_items
                WHERE menu_id IN (SELECT id FROM navigation.navigation_menus WHERE scope <> 'Admin');");

            migrationBuilder.DropForeignKey(name: "fk_navigation_items_navigation_menus_menu_id", schema: "navigation", table: "navigation_items");
            migrationBuilder.DropCheckConstraint(name: "ck_navigation_items_group_has_no_url", schema: "navigation", table: "navigation_items");
            migrationBuilder.DropCheckConstraint(name: "ck_navigation_items_not_own_parent", schema: "navigation", table: "navigation_items");
            migrationBuilder.DropIndex(name: "ix_navigation_items_menu_id_code", schema: "navigation", table: "navigation_items");
            migrationBuilder.DropIndex(name: "ix_navigation_items_menu_id_parent_id_sort_order", schema: "navigation", table: "navigation_items");

            RenameIndex(migrationBuilder, "ix_navigation_item_permissions_item_id_permission_code", "ix_menu_permissions_menu_id_permission_code");
            RenameConstraint(migrationBuilder, "navigation_item_permissions", "fk_navigation_item_permissions_navigation_items_item_id", "fk_menu_permissions_menus_menu_id");
            RenameConstraint(migrationBuilder, "navigation_item_permissions", "pk_navigation_item_permissions", "pk_menu_permissions");
            RenameIndex(migrationBuilder, "ix_navigation_items_parent_id", "ix_menus_parent_id");
            RenameConstraint(migrationBuilder, "navigation_items", "fk_navigation_items_navigation_items_parent_id", "fk_menus_menus_parent_id");
            RenameConstraint(migrationBuilder, "navigation_items", "pk_navigation_items", "pk_menus");

            migrationBuilder.RenameColumn(name: "item_id", schema: "navigation", table: "navigation_item_permissions", newName: "menu_id");
            migrationBuilder.RenameTable(name: "navigation_item_permissions", schema: "navigation", newName: "menu_permissions", newSchema: "navigation");

            migrationBuilder.DropColumn(name: "is_group", schema: "navigation", table: "navigation_items");
            migrationBuilder.DropColumn(name: "menu_id", schema: "navigation", table: "navigation_items");
            migrationBuilder.RenameColumn(name: "url", schema: "navigation", table: "navigation_items", newName: "route");
            migrationBuilder.RenameTable(name: "navigation_items", schema: "navigation", newName: "menus", newSchema: "navigation");

            migrationBuilder.CreateIndex(
                name: "ix_menus_code", schema: "navigation", table: "menus", column: "code", unique: true);

            migrationBuilder.DropTable(name: "navigation_menus", schema: "navigation");
        }

        // RenameTable keeps the old constraint/index names (the generated SQL is a bare ALTER TABLE ... RENAME TO),
        // so these renames are always needed. Renaming a primary key also renames its index.
        private static void RenameConstraint(MigrationBuilder migrationBuilder, string table, string oldName, string newName) =>
            migrationBuilder.Sql($"ALTER TABLE navigation.{table} RENAME CONSTRAINT {oldName} TO {newName};");

        private static void RenameIndex(MigrationBuilder migrationBuilder, string oldName, string newName) =>
            migrationBuilder.Sql($"ALTER INDEX navigation.{oldName} RENAME TO {newName};");
    }
}
