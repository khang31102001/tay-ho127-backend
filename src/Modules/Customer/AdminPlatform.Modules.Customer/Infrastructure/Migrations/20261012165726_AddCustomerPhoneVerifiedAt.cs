using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Customer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPhoneVerifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "phone_verified_at_utc",
                schema: "customer",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "phone_verified_at_utc",
                schema: "customer",
                table: "customers");
        }
    }
}
