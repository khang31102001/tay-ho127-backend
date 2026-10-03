using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Seo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeoMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seo_metadata",
                schema: "seo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    canonical_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    robots_index = table.Column<bool>(type: "boolean", nullable: false),
                    robots_follow = table.Column<bool>(type: "boolean", nullable: false),
                    og_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    og_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    og_image_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    twitter_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    twitter_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    twitter_image_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seo_metadata", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_seo_metadata_entity_type_entity_id",
                schema: "seo",
                table: "seo_metadata",
                columns: new[] { "entity_type", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seo_metadata_entity_type_homepage",
                schema: "seo",
                table: "seo_metadata",
                column: "entity_type",
                unique: true,
                filter: "entity_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seo_metadata",
                schema: "seo");
        }
    }
}
