using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddedTPHForUserToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_AspNetUsers_CustomerUserId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Companies_CompanyId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Customers_CustomerId",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Companies_CompanyId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_NationalId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "Customers");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "UserProfile");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "UserProfile",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "UserProfile",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25);

            migrationBuilder.AddColumn<string>(
                name: "Company_Name",
                table: "UserProfile",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserProfile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileType",
                table: "UserProfile",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxID",
                table: "UserProfile",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserProfile",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserProfile",
                table: "UserProfile",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_Company_Name",
                table: "UserProfile",
                column: "Company_Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_NationalId",
                table: "UserProfile",
                column: "NationalId",
                unique: true,
                filter: "[NationalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_TaxID",
                table: "UserProfile",
                column: "TaxID",
                unique: true,
                filter: "[TaxID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_UserId",
                table: "UserProfile",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_UserProfile_CompanyId",
                table: "Drivers",
                column: "CompanyId",
                principalTable: "UserProfile",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_UserProfile_CompanyId",
                table: "Offers",
                column: "CompanyId",
                principalTable: "UserProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_UserProfile_CustomerId",
                table: "Shipments",
                column: "CustomerId",
                principalTable: "UserProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfile_AspNetUsers_UserId",
                table: "UserProfile",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_UserProfile_CompanyId",
                table: "Vehicles",
                column: "CompanyId",
                principalTable: "UserProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_UserProfile_CompanyId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_UserProfile_CompanyId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_UserProfile_CustomerId",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfile_AspNetUsers_UserId",
                table: "UserProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_UserProfile_CompanyId",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserProfile",
                table: "UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_UserProfile_Company_Name",
                table: "UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_UserProfile_NationalId",
                table: "UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_UserProfile_TaxID",
                table: "UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_UserProfile_UserId",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Company_Name",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "ProfileType",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "TaxID",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserProfile");

            migrationBuilder.RenameTable(
                name: "UserProfile",
                newName: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Customers",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerUserId",
                table: "Customers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TaxID = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_AspNetUsers_CompanyUserId",
                        column: x => x.CompanyUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerUserId",
                table: "Customers",
                column: "CustomerUserId",
                unique: true,
                filter: "[CustomerUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NationalId",
                table: "Customers",
                column: "NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyUserId",
                table: "Companies",
                column: "CompanyUserId",
                unique: true,
                filter: "[CompanyUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TaxID",
                table: "Companies",
                column: "TaxID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_AspNetUsers_CustomerUserId",
                table: "Customers",
                column: "CustomerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Companies_CompanyId",
                table: "Offers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Customers_CustomerId",
                table: "Shipments",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Companies_CompanyId",
                table: "Vehicles",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
