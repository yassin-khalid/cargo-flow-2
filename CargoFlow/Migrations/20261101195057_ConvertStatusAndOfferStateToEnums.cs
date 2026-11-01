using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStatusAndOfferStateToEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "OfferStates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "UnderReview");

            migrationBuilder.UpdateData(
                table: "ShipmentStatuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "OpenForOffers");

            migrationBuilder.UpdateData(
                table: "ShipmentStatuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "OffersReceived");

            migrationBuilder.UpdateData(
                table: "ShipmentStatuses",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "InTransit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "OfferStates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Under Review");

            migrationBuilder.UpdateData(
                table: "ShipmentStatuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Open for Offers");

            migrationBuilder.UpdateData(
                table: "ShipmentStatuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Offers Received");

            migrationBuilder.UpdateData(
                table: "ShipmentStatuses",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "In Transit");
        }
    }
}
