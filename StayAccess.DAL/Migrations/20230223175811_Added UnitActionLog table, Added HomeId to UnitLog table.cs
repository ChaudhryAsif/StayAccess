using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class AddedUnitActionLogtableAddedHomeIdtoUnitLogtable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomeId",
                table: "UnitLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UnitActionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstallationId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slot = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitActionLog", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitActionLog");

            migrationBuilder.DropColumn(
                name: "HomeId",
                table: "UnitLog");
        }
    }
}
