using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScheduleOverride2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "ScheduleOverrides",
                newName: "ShuttleReason");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShuttleReason",
                table: "ScheduleOverrides",
                newName: "Reason");
        }
    }
}
