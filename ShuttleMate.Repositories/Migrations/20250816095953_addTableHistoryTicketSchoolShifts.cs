using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class addTableHistoryTicketSchoolShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistoryTicketSchoolShifts",
                columns: table => new
                {
                    HistoryTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_HistoryTicketSchoolShifts", x => new { x.HistoryTicketId, x.SchoolShiftId });
                    table.ForeignKey(
                        name: "FK_HistoryTicketSchoolShifts_HistoryTickets_HistoryTicketId",
                        column: x => x.HistoryTicketId,
                        principalTable: "HistoryTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoryTicketSchoolShifts_SchoolShifts_SchoolShiftId",
                        column: x => x.SchoolShiftId,
                        principalTable: "SchoolShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoryTicketSchoolShifts_SchoolShiftId",
                table: "HistoryTicketSchoolShifts",
                column: "SchoolShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoryTicketSchoolShifts");

            migrationBuilder.DropColumn(
                name: "AssignCode",
                table: "Users");
        }
    }
}
