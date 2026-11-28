using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class RenamedTrackingEventsOccurredAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OccuredAtUtc",
                table: "ShipmentTrackingEvents",
                newName: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OccurredAtUtc",
                table: "ShipmentTrackingEvents",
                newName: "OccuredAtUtc");
        }
    }
}
