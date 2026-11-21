using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerSectionLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banner_section_links",
                schema: "content",
                columns: table => new
                {
                    banner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_section_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_banner_section_links", x => new { x.banner_id, x.page_section_id });
                    table.ForeignKey(
                        name: "fk_banner_section_links_banners_banner_id",
                        column: x => x.banner_id,
                        principalSchema: "content",
                        principalTable: "banners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_banner_section_links_page_sections_page_section_id",
                        column: x => x.page_section_id,
                        principalSchema: "content",
                        principalTable: "page_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_banner_section_links_page_section_id",
                schema: "content",
                table: "banner_section_links",
                column: "page_section_id");

            // Banner có sẵn ở vị trí cố định HOME_PROMOTION → gắn vào section "promotion" của trang chủ (đổi thành section
            // banner); HOME_HERO / MENU_HERO chưa bao giờ được Site hiển thị nên chỉ chuyển sang "Sections" (admin gắn
            // section sau). ARTICLE_BANNER giữ nguyên: trang chi tiết bài viết là template, chưa có section.
            migrationBuilder.Sql(
                "UPDATE content.page_sections s SET kind = 'Banner' FROM content.pages p " +
                "WHERE s.page_id = p.id AND p.page_key = 'home' AND s.section_key = 'promotion' AND s.kind = 'Promotion';");
            migrationBuilder.Sql(
                "INSERT INTO content.banner_section_links (banner_id, page_section_id) " +
                "SELECT b.id, s.id FROM content.banners b " +
                "JOIN content.pages p ON p.page_key = 'home' " +
                "JOIN content.page_sections s ON s.page_id = p.id AND s.section_key = 'promotion' AND s.kind = 'Banner' " +
                "WHERE b.placement = 'HomePromotion';");
            migrationBuilder.Sql(
                "UPDATE content.banners SET placement = 'Sections' WHERE placement IN ('HomePromotion', 'HomeHero', 'MenuHero');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banner_section_links",
                schema: "content");
        }
    }
}
