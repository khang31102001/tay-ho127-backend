using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductCategoryNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_category_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_category_id",
                schema: "catalog",
                table: "products",
                column: "category_id",
                principalSchema: "catalog",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_category_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_category_id",
                schema: "catalog",
                table: "products",
                column: "category_id",
                principalSchema: "catalog",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
