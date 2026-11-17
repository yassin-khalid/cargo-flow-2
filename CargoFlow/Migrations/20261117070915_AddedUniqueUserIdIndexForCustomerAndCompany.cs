using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddedUniqueUserIdIndexForCustomerAndCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyUserId",
                table: "Companies");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers",
                column: "CustomerUserId",
                unique: true,
                filter: "[CustomerUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyUserId",
                table: "Companies",
                column: "CompanyUserId",
                unique: true,
                filter: "[CompanyUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyUserId",
                table: "Companies");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyUserId",
                table: "Companies",
                column: "CompanyUserId");
        }
    }
}
