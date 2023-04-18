using Microsoft.EntityFrameworkCore.Migrations;

namespace CoinsManagerService.Migrations
{
    public partial class initialmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoinTypes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Continents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Continent = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Continents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Continent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Countries_Continents",
                        column: x => x.Continent,
                        principalTable: "Continents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Periods",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Period = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Periods", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Periods_Countries",
                        column: x => x.Country,
                        principalTable: "Countries",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Coins",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nominal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CommemorativeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Period = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    PictPreviewPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coins", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Coins_Periods",
                        column: x => x.Period,
                        principalTable: "Periods",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Coins_Types",
                        column: x => x.Type,
                        principalTable: "CoinTypes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Coins_Period",
                table: "Coins",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Coins_Type",
                table: "Coins",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Continent",
                table: "Countries",
                column: "Continent");

            migrationBuilder.CreateIndex(
                name: "IX_Periods_Country",
                table: "Periods",
                column: "Country");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coins");

            migrationBuilder.DropTable(
                name: "Periods");

            migrationBuilder.DropTable(
                name: "CoinTypes");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Continents");
        }
    }
}
