using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class FixPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoryTickets_PromotionId",
                table: "HistoryTickets");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryTickets_PromotionId",
                table: "HistoryTickets",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoryTickets_PromotionId",
                table: "HistoryTickets");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryTickets_PromotionId",
                table: "HistoryTickets",
                column: "PromotionId",
                unique: true,
                filter: "[PromotionId] IS NOT NULL");
        }
    }
}
