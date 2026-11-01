using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageSectionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS: the column was added by hand on production before this migration shipped; there it is a no-op.
            migrationBuilder.Sql("ALTER TABLE content.page_sections ADD COLUMN IF NOT EXISTS settings jsonb NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE content.page_sections DROP COLUMN IF EXISTS settings;");
        }
    }
}
