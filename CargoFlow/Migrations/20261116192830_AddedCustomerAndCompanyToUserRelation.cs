using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddedCustomerAndCompanyToUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerUserId",
                table: "Customers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyUserId",
                table: "Companies",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyUserId",
                table: "Companies",
                column: "CompanyUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_AspNetUsers_CompanyUserId",
                table: "Companies",
                column: "CompanyUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_AspNetUsers_CustomerUserId",
                table: "Customers",
                column: "CustomerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_AspNetUsers_CompanyUserId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_AspNetUsers_CustomerUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CompanyUserId",
                table: "Companies");
        }
    }
}
