using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Seo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRedirects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "redirects",
                schema: "seo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    destination_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    redirect_type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_redirects", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_redirects_source_path",
                schema: "seo",
                table: "redirects",
                column: "source_path",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redirects",
                schema: "seo");
        }
    }
}
