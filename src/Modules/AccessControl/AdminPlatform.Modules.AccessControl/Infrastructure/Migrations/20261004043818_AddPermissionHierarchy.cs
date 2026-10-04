using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.AccessControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_group",
                schema: "access_control",
                table: "permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_id",
                schema: "access_control",
                table: "permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                schema: "access_control",
                table: "permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_parent_id_sort_order",
                schema: "access_control",
                table: "permissions",
                columns: new[] { "parent_id", "sort_order" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_permissions_not_own_parent",
                schema: "access_control",
                table: "permissions",
                sql: "parent_id IS NULL OR parent_id <> id");

            migrationBuilder.AddForeignKey(
                name: "fk_permissions_permissions_parent_id",
                schema: "access_control",
                table: "permissions",
                column: "parent_id",
                principalSchema: "access_control",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_permissions_permissions_parent_id",
                schema: "access_control",
                table: "permissions");

            migrationBuilder.DropIndex(
                name: "ix_permissions_parent_id_sort_order",
                schema: "access_control",
                table: "permissions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permissions_not_own_parent",
                schema: "access_control",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "is_group",
                schema: "access_control",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "parent_id",
                schema: "access_control",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "access_control",
                table: "permissions");
        }
    }
}
