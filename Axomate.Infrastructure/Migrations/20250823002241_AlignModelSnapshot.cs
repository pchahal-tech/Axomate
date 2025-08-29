using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MileageHistory_Mileage_Range",
                table: "MileageHistories");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MileageHistory_Mileage_Range",
                table: "MileageHistories",
                sql: "Mileage >= 0 AND Mileage <= 2000000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MileageHistory_Mileage_Range",
                table: "MileageHistories");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MileageHistory_Mileage_Range",
                table: "MileageHistories",
                sql: "[Mileage] >= 0 AND [Mileage] <= 2000000");
        }
    }
}
