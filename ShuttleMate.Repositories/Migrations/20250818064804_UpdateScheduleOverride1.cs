using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScheduleOverride1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverReason",
                table: "ScheduleOverrides",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverReason",
                table: "ScheduleOverrides");
        }
    }
}
