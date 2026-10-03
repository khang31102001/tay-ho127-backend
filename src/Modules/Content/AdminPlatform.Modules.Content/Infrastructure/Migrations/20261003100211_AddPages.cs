using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pages",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "page_sections",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    eyebrow = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    heading = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    subheading = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    cta_label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cta_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_page_sections_pages_page_id",
                        column: x => x.page_id,
                        principalSchema: "content",
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_page_sections_page_id_display_order",
                schema: "content",
                table: "page_sections",
                columns: new[] { "page_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_pages_slug",
                schema: "content",
                table: "pages",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "page_sections",
                schema: "content");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "content");
        }
    }
}
