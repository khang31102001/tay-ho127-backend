using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.CreateTable(
                name: "delivery_logs",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    result_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    request_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    masked_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "otp_challenges",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    request_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_otp_challenges", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_logs_created_at_utc",
                schema: "notification",
                table: "delivery_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_logs_reference_id",
                schema: "notification",
                table: "delivery_logs",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_otp_challenges_created_at_utc",
                schema: "notification",
                table: "otp_challenges",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_otp_challenges_phone_created_at_utc",
                schema: "notification",
                table: "otp_challenges",
                columns: new[] { "phone", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_otp_challenges_request_ip_created_at_utc",
                schema: "notification",
                table: "otp_challenges",
                columns: new[] { "request_ip", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_logs",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "otp_challenges",
                schema: "notification");
        }
    }
}
