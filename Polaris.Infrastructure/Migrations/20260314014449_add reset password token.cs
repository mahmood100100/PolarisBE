using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Polaris.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addresetpasswordtoken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ResePasswordTokenUsed",
                table: "AspNetUsers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetPasswordToken",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetPasswordTokenExpiresAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResePasswordTokenUsed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ResetPasswordToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ResetPasswordTokenExpiresAt",
                table: "AspNetUsers");
        }
    }
}
