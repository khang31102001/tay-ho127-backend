using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaStorageProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "storage_provider",
                schema: "media",
                table: "media",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "External");

            // Media có sẵn: file upload mang tên lưu trữ là Guid 32 ký tự hex (MediaService.UploadAsync) dưới
            // /uploads/ hoặc nằm trên res.cloudinary.com; còn lại là URL Admin nhập tay.
            migrationBuilder.Sql(
                "UPDATE media.media SET storage_provider = 'Local' " +
                @"WHERE url ~ '/uploads/[0-9a-f]{32}\.[a-z0-9]+$';");
            migrationBuilder.Sql(
                "UPDATE media.media SET storage_provider = 'Cloudinary' " +
                "WHERE url LIKE 'https://res.cloudinary.com/%';");

            migrationBuilder.CreateIndex(
                name: "ix_media_storage_provider",
                schema: "media",
                table: "media",
                column: "storage_provider");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_media_storage_provider",
                schema: "media",
                table: "media");

            migrationBuilder.DropColumn(
                name: "storage_provider",
                schema: "media",
                table: "media");
        }
    }
}
