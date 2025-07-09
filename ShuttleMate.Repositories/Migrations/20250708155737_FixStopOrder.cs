using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class FixStopOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopOrder",
                table: "Stops");

            migrationBuilder.AddColumn<int>(
                name: "StopOrder",
                table: "RouteStops",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopOrder",
                table: "RouteStops");

            migrationBuilder.AddColumn<int>(
                name: "StopOrder",
                table: "Stops",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
