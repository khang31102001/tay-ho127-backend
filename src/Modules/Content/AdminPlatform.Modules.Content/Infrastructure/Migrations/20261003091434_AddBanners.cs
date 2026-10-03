using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banners",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    desktop_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    mobile_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    alt_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    heading = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    subheading = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cta_label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cta_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    placement = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    start_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_banners", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_banners_placement_is_active_display_order",
                schema: "content",
                table: "banners",
                columns: new[] { "placement", "is_active", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banners",
                schema: "content");
        }
    }
}
