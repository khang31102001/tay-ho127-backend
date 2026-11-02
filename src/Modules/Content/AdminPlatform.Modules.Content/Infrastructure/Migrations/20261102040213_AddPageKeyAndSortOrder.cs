using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageKeyAndSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS / NOT EXISTS everywhere: run by hand on production before this migration shipped
            // (document/04-nghiep-vu/de-xuat/QUAN-LY-SECTION-TRANG.md); there it is a no-op.
            migrationBuilder.Sql("ALTER TABLE content.pages ADD COLUMN IF NOT EXISTS page_key character varying(64) NULL;");
            migrationBuilder.Sql("ALTER TABLE content.pages ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;");

            migrationBuilder.Sql("ALTER TABLE content.pages DROP CONSTRAINT IF EXISTS ck_pages_page_key_format;");
            migrationBuilder.Sql("""
                ALTER TABLE content.pages ADD CONSTRAINT ck_pages_page_key_format
                CHECK (page_key IS NULL OR page_key ~ '^[a-z0-9]+(-[a-z0-9]+)*$');
                """);

            // The pages the website renders get the key it now looks them up by.
            migrationBuilder.Sql("""
                UPDATE content.pages p
                SET page_key = m.page_key, sort_order = m.sort_order
                FROM (VALUES ('/', 'home', 10), ('/gioi-thieu', 'gioi-thieu', 20), ('/thuc-don', 'thuc-don', 30),
                             ('/tin-tuc', 'tin-tuc', 40), ('/lien-he', 'lien-he', 50)) AS m(slug, page_key, sort_order)
                WHERE p.slug = m.slug AND p.page_key IS NULL
                  AND NOT EXISTS (SELECT 1 FROM content.pages x WHERE x.page_key = m.page_key);
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS ix_pages_page_key
                ON content.pages (page_key) WHERE page_key IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pages_page_key",
                schema: "content",
                table: "pages");

            migrationBuilder.Sql("ALTER TABLE content.pages DROP CONSTRAINT IF EXISTS ck_pages_page_key_format;");

            migrationBuilder.DropColumn(
                name: "page_key",
                schema: "content",
                table: "pages");

            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "content",
                table: "pages");
        }
    }
}
