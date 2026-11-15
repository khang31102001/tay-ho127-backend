using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Catalog.Infrastructure.Migrations
{
    /// <summary>Ảnh đại diện sản phẩm: catalog.products.thumbnail_media_id (uuid, null được) + FK tới
    /// media.media(id) ON DELETE SET NULL. Viết bằng SQL idempotent vì cột có thể đã được tạo tay trên DB
    /// (DBeaver) trước khi có migration này — chạy lại không lỗi, không tạo cột/FK trùng.</summary>
    public partial class AddProductThumbnailMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE catalog.products ADD COLUMN IF NOT EXISTS thumbnail_media_id uuid NULL;");

            // Giá trị (nếu có, do tạo tay) trỏ tới media không tồn tại thì bỏ trước khi gắn FK.
            migrationBuilder.Sql(
                "UPDATE catalog.products p SET thumbnail_media_id = NULL " +
                "WHERE thumbnail_media_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM media.media m WHERE m.id = p.thumbnail_media_id);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_products_thumbnail_media_id ON catalog.products (thumbnail_media_id);");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint c
        JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY (c.conkey)
        WHERE c.contype = 'f'
          AND c.conrelid = 'catalog.products'::regclass
          AND c.confrelid = 'media.media'::regclass
          AND a.attname = 'thumbnail_media_id')
    THEN
        ALTER TABLE catalog.products
            ADD CONSTRAINT fk_products_media_thumbnail_media_id
            FOREIGN KEY (thumbnail_media_id) REFERENCES media.media (id) ON DELETE SET NULL;
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE catalog.products DROP CONSTRAINT IF EXISTS fk_products_media_thumbnail_media_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS catalog.ix_products_thumbnail_media_id;");
            migrationBuilder.Sql("ALTER TABLE catalog.products DROP COLUMN IF EXISTS thumbnail_media_id;");
        }
    }
}
