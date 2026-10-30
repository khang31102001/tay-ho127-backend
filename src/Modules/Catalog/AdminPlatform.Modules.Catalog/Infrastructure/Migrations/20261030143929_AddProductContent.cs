using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS: the column was first added by hand on an existing database; this keeps the
            // migration safe to run there while still creating it on fresh databases.
            migrationBuilder.Sql("ALTER TABLE catalog.products ADD COLUMN IF NOT EXISTS content text NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE catalog.products DROP COLUMN IF EXISTS content;");
        }
    }
}
