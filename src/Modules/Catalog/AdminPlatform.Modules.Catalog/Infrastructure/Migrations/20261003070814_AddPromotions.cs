using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotions",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    max_discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    start_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    usage_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotions", x => x.id);
                    table.CheckConstraint("ck_promotions_max_discount_non_negative", "max_discount_amount IS NULL OR max_discount_amount >= 0");
                    table.CheckConstraint("ck_promotions_minimum_order_non_negative", "minimum_order_amount IS NULL OR minimum_order_amount >= 0");
                    table.CheckConstraint("ck_promotions_usage_count_non_negative", "usage_count >= 0");
                    table.CheckConstraint("ck_promotions_usage_limit_positive", "usage_limit IS NULL OR usage_limit >= 1");
                    table.CheckConstraint("ck_promotions_value_positive", "value > 0");
                });

            migrationBuilder.CreateTable(
                name: "promotion_categories",
                schema: "catalog",
                columns: table => new
                {
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_categories", x => new { x.promotion_id, x.category_id });
                    table.ForeignKey(
                        name: "fk_promotion_categories_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_promotion_categories_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "catalog",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_products",
                schema: "catalog",
                columns: table => new
                {
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_products", x => new { x.promotion_id, x.product_id });
                    table.ForeignKey(
                        name: "fk_promotion_products_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_promotion_products_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "catalog",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_categories_category_id",
                schema: "catalog",
                table: "promotion_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_products_product_id",
                schema: "catalog",
                table: "promotion_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_code",
                schema: "catalog",
                table: "promotions",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "promotion_products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "promotions",
                schema: "catalog");
        }
    }
}
