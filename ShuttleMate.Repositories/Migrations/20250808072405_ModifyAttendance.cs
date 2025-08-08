using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ModifyAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CheckOutLocation",
                table: "Attendances",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CheckInLocation",
                table: "Attendances",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckInLocation",
                table: "Attendances",
                column: "CheckInLocation");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckOutLocation",
                table: "Attendances",
                column: "CheckOutLocation");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Stops_CheckInLocation",
                table: "Attendances",
                column: "CheckInLocation",
                principalTable: "Stops",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Stops_CheckOutLocation",
                table: "Attendances",
                column: "CheckOutLocation",
                principalTable: "Stops",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Stops_CheckInLocation",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Stops_CheckOutLocation",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckInLocation",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckOutLocation",
                table: "Attendances");

            migrationBuilder.AlterColumn<string>(
                name: "CheckOutLocation",
                table: "Attendances",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CheckInLocation",
                table: "Attendances",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
