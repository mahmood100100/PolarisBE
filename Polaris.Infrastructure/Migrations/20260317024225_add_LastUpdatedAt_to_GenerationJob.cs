using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Polaris.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_LastUpdatedAt_to_GenerationJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "GenerationJobs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "GenerationJobs");
        }
    }
}
