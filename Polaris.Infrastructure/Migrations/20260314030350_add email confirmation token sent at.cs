using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Polaris.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addemailconfirmationtokensentat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenSentAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenSentAt",
                table: "AspNetUsers");
        }
    }
}
