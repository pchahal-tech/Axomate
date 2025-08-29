using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecuritySidecars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_LicensePlate",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_VIN",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Phone",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "VIN",
                table: "Vehicles",
                type: "TEXT",
                maxLength: 17,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 17,
                oldNullable: true,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "LicensePlate",
                table: "Vehicles",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldNullable: true,
                oldCollation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "LicensePlateHash",
                table: "Vehicles",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VinHash",
                table: "Vehicles",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailHash",
                table: "Customers",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneHash",
                table: "Customers",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LicensePlateHash",
                table: "Vehicles",
                column: "LicensePlateHash",
                unique: true,
                filter: "LicensePlateHash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VinHash",
                table: "Vehicles",
                column: "VinHash",
                unique: true,
                filter: "VinHash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_EmailHash",
                table: "Customers",
                column: "EmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneHash",
                table: "Customers",
                column: "PhoneHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_LicensePlateHash",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_VinHash",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Customers_EmailHash",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PhoneHash",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LicensePlateHash",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VinHash",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EmailHash",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PhoneHash",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "VIN",
                table: "Vehicles",
                type: "TEXT",
                maxLength: 17,
                nullable: true,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 17,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LicensePlate",
                table: "Vehicles",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LicensePlate",
                table: "Vehicles",
                column: "LicensePlate",
                unique: true,
                filter: "LicensePlate IS NOT NULL AND length(trim(LicensePlate)) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VIN",
                table: "Vehicles",
                column: "VIN",
                unique: true,
                filter: "VIN IS NOT NULL AND length(trim(VIN)) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");
        }
    }
}
