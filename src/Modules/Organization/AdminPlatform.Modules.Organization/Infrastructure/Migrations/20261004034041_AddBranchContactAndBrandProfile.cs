using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Organization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchContactAndBrandProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line",
                schema: "organization",
                table: "brands",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "business_hours_note",
                schema: "organization",
                table: "brands",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "close_time",
                schema: "organization",
                table: "brands",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "district",
                schema: "organization",
                table: "brands",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "organization",
                table: "brands",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hotline",
                schema: "organization",
                table: "brands",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                schema: "organization",
                table: "brands",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "open_time",
                schema: "organization",
                table: "brands",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                schema: "organization",
                table: "brands",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                schema: "organization",
                table: "brands",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ward",
                schema: "organization",
                table: "brands",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "brand_profiles",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tagline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    legal_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    social_links = table.Column<string>(type: "jsonb", nullable: false),
                    logo_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    logo_dark_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    logo_light_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    favicon_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    og_image_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brand_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_brands_is_primary",
                schema: "organization",
                table: "brands",
                column: "is_primary",
                unique: true,
                filter: "is_primary");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_profiles",
                schema: "organization");

            migrationBuilder.DropIndex(
                name: "ix_brands_is_primary",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "address_line",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "business_hours_note",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "close_time",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "district",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "hotline",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "is_primary",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "open_time",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "phone",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "province",
                schema: "organization",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "ward",
                schema: "organization",
                table: "brands");
        }
    }
}
