using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddedWightTypeNameEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "WeightTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Kilogram");

            migrationBuilder.UpdateData(
                table: "WeightTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Ton");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "WeightTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Kilogram (kg)");

            migrationBuilder.UpdateData(
                table: "WeightTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Ton (t)");
        }
    }
}
