using Microsoft.EntityFrameworkCore.Migrations;

namespace CoinsManagerService.Migrations
{
    public partial class RenameCountryPeriodContinentColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Period",
                table: "Periods",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "Countries",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Continent",
                table: "Continents",
                newName: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Periods",
                newName: "Period");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Countries",
                newName: "Country");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Continents",
                newName: "Continent");
        }
    }
}
