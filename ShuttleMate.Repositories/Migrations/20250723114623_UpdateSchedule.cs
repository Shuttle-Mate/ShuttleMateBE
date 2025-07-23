using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolShiftId",
                table: "Schedules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SchoolShiftId",
                table: "Schedules",
                column: "SchoolShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_SchoolShifts_SchoolShiftId",
                table: "Schedules",
                column: "SchoolShiftId",
                principalTable: "SchoolShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_SchoolShifts_SchoolShiftId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SchoolShiftId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "SchoolShiftId",
                table: "Schedules");
        }
    }
}
