using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddedNotificationUserProfileRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Shipments_ShipmentId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_RecipientType_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RecipientType",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_ReadAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "ReadAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Shipments_ShipmentId",
                table: "Notifications",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_UserProfile_RecipientId",
                table: "Notifications",
                column: "RecipientId",
                principalTable: "UserProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Shipments_ShipmentId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_UserProfile_RecipientId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_ReadAt",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "RecipientType",
                table: "Notifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_RecipientType_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "RecipientType", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Shipments_ShipmentId",
                table: "Notifications",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "Id");
        }
    }
}
