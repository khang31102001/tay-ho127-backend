using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageSectionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS / NOT EXISTS everywhere: the column, its check, the backfill and the index were run by hand on
            // production before this migration shipped (document/04-nghiep-vu/de-xuat/QUAN-LY-SECTION-TRANG.md); there it is a no-op.
            migrationBuilder.Sql("ALTER TABLE content.page_sections ADD COLUMN IF NOT EXISTS section_key character varying(64) NULL;");

            migrationBuilder.Sql("ALTER TABLE content.page_sections DROP CONSTRAINT IF EXISTS ck_page_sections_section_key_format;");
            migrationBuilder.Sql("""
                ALTER TABLE content.page_sections ADD CONSTRAINT ck_page_sections_section_key_format
                CHECK (section_key IS NULL OR section_key ~ '^[a-z0-9]+(-[a-z0-9]+)*$');
                """);

            // The home page sections the website already reads by kind get the slot key it now reads them by
            // (the first section of each kind, by display order).
            migrationBuilder.Sql("""
                UPDATE content.page_sections s
                SET section_key = m.new_key
                FROM content.pages p,
                     (VALUES ('Hero', 'hero'), ('Story', 'story'), ('Promotion', 'promotion'),
                             ('Highlight', 'favorites'), ('Award', 'award'), ('Testimonial', 'testimonials')) AS m(kind, new_key)
                WHERE p.id = s.page_id AND p.slug = '/' AND s.kind = m.kind
                  AND s.section_key IS NULL
                  AND s.id = (SELECT x.id FROM content.page_sections x
                              WHERE x.page_id = s.page_id AND x.kind = s.kind
                              ORDER BY x.display_order, x.created_at_utc LIMIT 1)
                  AND NOT EXISTS (SELECT 1 FROM content.page_sections y
                                  WHERE y.page_id = s.page_id AND y.section_key = m.new_key);
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS ix_page_sections_page_id_section_key
                ON content.page_sections (page_id, section_key) WHERE section_key IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_page_sections_page_id_section_key",
                schema: "content",
                table: "page_sections");

            migrationBuilder.Sql("ALTER TABLE content.page_sections DROP CONSTRAINT IF EXISTS ck_page_sections_section_key_format;");

            migrationBuilder.DropColumn(
                name: "section_key",
                schema: "content",
                table: "page_sections");
        }
    }
}
