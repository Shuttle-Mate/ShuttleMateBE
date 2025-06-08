using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeInPaymentAndHistoryTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoryTickets_Transactions_TransactionId",
                table: "HistoryTickets");

            migrationBuilder.DropIndex(
                name: "IX_HistoryTickets_TransactionId",
                table: "HistoryTickets");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "HistoryTickets");

            migrationBuilder.AddColumn<string>(
                name: "BuyerAddress",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerEmail",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerName",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerPhone",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HistoryTicketId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderCode",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "HistoryTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_HistoryTicketId",
                table: "Transactions",
                column: "HistoryTicketId",
                unique: true,
                filter: "[HistoryTicketId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_HistoryTickets_HistoryTicketId",
                table: "Transactions",
                column: "HistoryTicketId",
                principalTable: "HistoryTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_HistoryTickets_HistoryTicketId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_HistoryTicketId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BuyerAddress",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BuyerEmail",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BuyerName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BuyerPhone",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "HistoryTicketId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "HistoryTickets");

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "HistoryTickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_HistoryTickets_TransactionId",
                table: "HistoryTickets",
                column: "TransactionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoryTickets_Transactions_TransactionId",
                table: "HistoryTickets",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
