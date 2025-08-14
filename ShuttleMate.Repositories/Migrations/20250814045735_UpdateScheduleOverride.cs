using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScheduleOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleOverrides_Shuttles_ShuttleId",
                table: "ScheduleOverrides");

            migrationBuilder.RenameColumn(
                name: "ShuttleId",
                table: "ScheduleOverrides",
                newName: "OriginalShuttleId");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleOverrides_ShuttleId",
                table: "ScheduleOverrides",
                newName: "IX_ScheduleOverrides_OriginalShuttleId");

            migrationBuilder.AlterColumn<Guid>(
                name: "OverrideUserId",
                table: "ScheduleOverrides",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "OriginalUserId",
                table: "ScheduleOverrides",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OverrideShuttleId",
                table: "ScheduleOverrides",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleOverrides_OverrideShuttleId",
                table: "ScheduleOverrides",
                column: "OverrideShuttleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleOverrides_Shuttles_OriginalShuttleId",
                table: "ScheduleOverrides",
                column: "OriginalShuttleId",
                principalTable: "Shuttles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleOverrides_Shuttles_OverrideShuttleId",
                table: "ScheduleOverrides",
                column: "OverrideShuttleId",
                principalTable: "Shuttles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleOverrides_Shuttles_OriginalShuttleId",
                table: "ScheduleOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleOverrides_Shuttles_OverrideShuttleId",
                table: "ScheduleOverrides");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleOverrides_OverrideShuttleId",
                table: "ScheduleOverrides");

            migrationBuilder.DropColumn(
                name: "OverrideShuttleId",
                table: "ScheduleOverrides");

            migrationBuilder.RenameColumn(
                name: "OriginalShuttleId",
                table: "ScheduleOverrides",
                newName: "ShuttleId");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleOverrides_OriginalShuttleId",
                table: "ScheduleOverrides",
                newName: "IX_ScheduleOverrides_ShuttleId");

            migrationBuilder.AlterColumn<Guid>(
                name: "OverrideUserId",
                table: "ScheduleOverrides",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OriginalUserId",
                table: "ScheduleOverrides",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleOverrides_Shuttles_ShuttleId",
                table: "ScheduleOverrides",
                column: "ShuttleId",
                principalTable: "Shuttles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
