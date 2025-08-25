using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class removebuildingunitfromreservationcode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationCode_BuildingUnit_BuildingUnitId",
                table: "ReservationCode");

            migrationBuilder.DropIndex(
                name: "IX_ReservationCode_BuildingUnitId",
                table: "ReservationCode");

            migrationBuilder.DropColumn(
                name: "BuildingUnitId",
                table: "ReservationCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuildingUnitId",
                table: "ReservationCode",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCode_BuildingUnitId",
                table: "ReservationCode",
                column: "BuildingUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationCode_BuildingUnit_BuildingUnitId",
                table: "ReservationCode",
                column: "BuildingUnitId",
                principalTable: "BuildingUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
