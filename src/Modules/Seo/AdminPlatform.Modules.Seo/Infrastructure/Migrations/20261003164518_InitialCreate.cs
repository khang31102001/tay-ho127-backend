using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Seo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "seo");

            migrationBuilder.CreateTable(
                name: "seo_settings",
                schema: "seo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_title_template = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    default_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    default_og_image_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    twitter_site = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitter_creator = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    default_robots_index = table.Column<bool>(type: "boolean", nullable: false),
                    default_robots_follow = table.Column<bool>(type: "boolean", nullable: false),
                    robots_disallow_paths = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seo_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seo_settings",
                schema: "seo");
        }
    }
}
