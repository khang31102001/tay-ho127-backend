using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Organization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "brands",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                    table.ForeignKey(
                        name: "fk_brands_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organization",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                    table.ForeignKey(
                        name: "fk_departments_departments_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "organization",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_departments_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organization",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_brands",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_brands", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_brands_brands_brand_id",
                        column: x => x.brand_id,
                        principalSchema: "organization",
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_departments",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_departments", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_departments_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "organization",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_brands_organization_id_code",
                schema: "organization",
                table: "brands",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_departments_organization_id_code",
                schema: "organization",
                table: "departments",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_departments_parent_id",
                schema: "organization",
                table: "departments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_code",
                schema: "organization",
                table: "organizations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_brands_brand_id",
                schema: "organization",
                table: "user_brands",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_brands_user_id_brand_id",
                schema: "organization",
                table: "user_brands",
                columns: new[] { "user_id", "brand_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_departments_department_id",
                schema: "organization",
                table: "user_departments",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_departments_user_id_department_id",
                schema: "organization",
                table: "user_departments",
                columns: new[] { "user_id", "department_id" },
                unique: true);

            // Cross-module references by id only (architecture assumption #6) — see the matching comment
            // in AccessControl's InitialCreate migration.
            migrationBuilder.Sql(
                "ALTER TABLE organization.user_departments " +
                "ADD CONSTRAINT fk_user_departments_identity_users_user_id " +
                "FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE;");
            migrationBuilder.Sql(
                "ALTER TABLE organization.user_brands " +
                "ADD CONSTRAINT fk_user_brands_identity_users_user_id " +
                "FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_brands",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "user_departments",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "brands",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "organization");
        }
    }
}
