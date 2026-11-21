using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "media",
                table: "media",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "media_categories",
                schema: "media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_categories", x => x.id);
                });

            // Nhóm hệ thống: code cố định (khớp MediaCategoryCodes ở code và MEDIA_CATEGORY_CODES ở FE), id cố định để
            // mọi môi trường giống nhau. Media có sẵn để category_id = null; MediaCategoryBackfill (Migrator seed)
            // gán nhóm theo nơi đang dùng.
            migrationBuilder.Sql(
                "INSERT INTO media.media_categories (id, code, name, sort_order, created_at_utc) VALUES " +
                "('00000000-0000-0000-0000-000000000001', 'products', 'Sản phẩm', 10, now()), " +
                "('00000000-0000-0000-0000-000000000002', 'articles', 'Bài viết', 20, now()), " +
                "('00000000-0000-0000-0000-000000000003', 'banners', 'Banner', 30, now()), " +
                "('00000000-0000-0000-0000-000000000004', 'page-sections', 'Section trang', 40, now()), " +
                "('00000000-0000-0000-0000-000000000005', 'brand', 'Thương hiệu', 50, now()), " +
                "('00000000-0000-0000-0000-000000000006', 'customers', 'Khách hàng', 60, now()), " +
                "('00000000-0000-0000-0000-000000000007', 'payments', 'Thanh toán', 70, now()), " +
                "('00000000-0000-0000-0000-000000000008', 'seo', 'SEO', 80, now()), " +
                "('00000000-0000-0000-0000-000000000009', 'general', 'Chung', 90, now());");

            migrationBuilder.CreateIndex(
                name: "ix_media_category_id",
                schema: "media",
                table: "media",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_categories_code",
                schema: "media",
                table: "media_categories",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_media_media_categories_category_id",
                schema: "media",
                table: "media",
                column: "category_id",
                principalSchema: "media",
                principalTable: "media_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_media_media_categories_category_id",
                schema: "media",
                table: "media");

            migrationBuilder.DropTable(
                name: "media_categories",
                schema: "media");

            migrationBuilder.DropIndex(
                name: "ix_media_category_id",
                schema: "media",
                table: "media");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "media",
                table: "media");
        }
    }
}
