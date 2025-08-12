using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuttleMate.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Notifications",
                newName: "TemplateType");

            migrationBuilder.AddColumn<int>(
                name: "NotificationCategory",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationCategory",
                table: "NotificationRecipients",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationCategory",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NotificationCategory",
                table: "NotificationRecipients");

            migrationBuilder.RenameColumn(
                name: "TemplateType",
                table: "Notifications",
                newName: "Type");
        }
    }
}
