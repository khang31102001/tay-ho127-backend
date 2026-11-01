using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleFeatured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The columns were already added by hand on production; IF NOT EXISTS lets this migration
            // run there as well as on a fresh database (local, new environments).
            migrationBuilder.Sql("""
                ALTER TABLE content.articles
                    ADD COLUMN IF NOT EXISTS is_featured boolean NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS featured_order integer NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "featured_order",
                schema: "content",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "is_featured",
                schema: "content",
                table: "articles");
        }
    }
}
