using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketPromo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketPromotions_TicketTypes_TicketTypeId",
                table: "TicketPromotions");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketPromotions_Promotions_PromotionId1",
                table: "TicketPromotions");

            migrationBuilder.DropIndex(
                name: "IX_TicketPromotions_PromotionId1",
                table: "TicketPromotions");

            migrationBuilder.DropColumn(
                name: "PromotionId1",
                table: "TicketPromotions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TicketPromotions",
                table: "TicketPromotions");

            migrationBuilder.AlterColumn<Guid>(
                name: "TicketId",
                table: "TicketPromotions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "PromotionId",
                table: "TicketPromotions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TicketPromotions",
                table: "TicketPromotions",
                columns: new[] { "TicketId", "PromotionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TicketPromotions_TicketTypes_TicketId",
                table: "TicketPromotions",
                column: "TicketId",
                principalTable: "TicketTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketPromotions_Promotions_PromotionId",
                table: "TicketPromotions",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketPromotions_Promotions_PromotionId",
                table: "TicketPromotions");

            migrationBuilder.AlterColumn<string>(
                name: "TicketId",
                table: "TicketPromotions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "PromotionId",
                table: "TicketPromotions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId1",
                table: "TicketPromotions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TicketPromotions_PromotionId1",
                table: "TicketPromotions",
                column: "PromotionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketPromotions_Promotions_PromotionId1",
                table: "TicketPromotions",
                column: "PromotionId1",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
