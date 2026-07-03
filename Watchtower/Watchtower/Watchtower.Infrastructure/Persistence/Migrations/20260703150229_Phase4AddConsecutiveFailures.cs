using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Watchtower.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4AddConsecutiveFailures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "Endpoints",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "Endpoints");
        }
    }
}
