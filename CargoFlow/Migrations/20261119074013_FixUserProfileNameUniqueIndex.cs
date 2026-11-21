using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class FixUserProfileNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfile_Company_Name",
                table: "UserProfile");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_Company_Name",
                table: "UserProfile",
                column: "Company_Name",
                unique: true,
                filter: "[Company_Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_Name",
                table: "UserProfile",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfile_Company_Name",
                table: "UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_UserProfile_Name",
                table: "UserProfile");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_Company_Name",
                table: "UserProfile",
                column: "Company_Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }
    }
}
