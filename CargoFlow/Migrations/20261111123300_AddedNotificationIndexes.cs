using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddedNotificationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_RecipientType_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "RecipientType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ShipmentId",
                table: "Notifications",
                column: "ShipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Shipments_ShipmentId",
                table: "Notifications",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Shipments_ShipmentId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_RecipientType_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ShipmentId",
                table: "Notifications");
        }
    }
}
