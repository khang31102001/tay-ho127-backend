using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "article_categories",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_article_categories_article_categories_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "content",
                        principalTable: "article_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "article_tags",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    featured_media_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_articles_article_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "content",
                        principalTable: "article_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "article_tag_links",
                schema: "content",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_tag_links", x => new { x.article_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_article_tag_links_article_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "content",
                        principalTable: "article_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_tag_links_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "content",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_categories_parent_id",
                schema: "content",
                table: "article_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_categories_slug",
                schema: "content",
                table: "article_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_tag_links_tag_id",
                schema: "content",
                table: "article_tag_links",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_tags_slug",
                schema: "content",
                table: "article_tags",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_category_id",
                schema: "content",
                table: "articles",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug",
                schema: "content",
                table: "articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_status_published_at_utc",
                schema: "content",
                table: "articles",
                columns: new[] { "status", "published_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_tag_links",
                schema: "content");

            migrationBuilder.DropTable(
                name: "article_tags",
                schema: "content");

            migrationBuilder.DropTable(
                name: "articles",
                schema: "content");

            migrationBuilder.DropTable(
                name: "article_categories",
                schema: "content");
        }
    }
}
