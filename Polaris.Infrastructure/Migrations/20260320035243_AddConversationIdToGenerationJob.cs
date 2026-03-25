using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Polaris.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationIdToGenerationJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "GenerationJobs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "GenerationJobs");
        }
    }
}
