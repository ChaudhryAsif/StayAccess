using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class addNodeIdColumnInUnitLogUnitSlotLogEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NodeId",
                table: "UnitSlotLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NodeId",
                table: "UnitLog",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "UnitSlotLog");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "UnitLog");
        }
    }
}
