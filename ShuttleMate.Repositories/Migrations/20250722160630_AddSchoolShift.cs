using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfternoonEndTime",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "AfternoonStartTime",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "MorningEndTime",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "MorningStartTime",
                table: "Schools");

            migrationBuilder.CreateTable(
                name: "SchoolShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Time = table.Column<TimeOnly>(type: "time", nullable: false),
                    ShiftType = table.Column<int>(type: "int", nullable: false),
                    SessionType = table.Column<int>(type: "int", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetaData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolShifts_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSchoolShifts",
                columns: table => new
                {
                    SchoolShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetaData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSchoolShifts", x => new { x.StudentId, x.SchoolShiftId });
                    table.ForeignKey(
                        name: "FK_UserSchoolShifts_SchoolShifts_SchoolShiftId",
                        column: x => x.SchoolShiftId,
                        principalTable: "SchoolShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSchoolShifts_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolShifts_SchoolId",
                table: "SchoolShifts",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSchoolShifts_SchoolShiftId",
                table: "UserSchoolShifts",
                column: "SchoolShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSchoolShifts");

            migrationBuilder.DropTable(
                name: "SchoolShifts");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonEndTime",
                table: "Schools",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonStartTime",
                table: "Schools",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningEndTime",
                table: "Schools",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningStartTime",
                table: "Schools",
                type: "time",
                nullable: true);
        }
    }
}
