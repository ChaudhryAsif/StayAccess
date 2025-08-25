using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class changesforlatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_BuildingUnit_BuildingUnitId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationCode_Reservation_ReservationId",
                table: "ReservationCode");

            migrationBuilder.CreateTable(
                name: "LockKey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingUnitId = table.Column<int>(type: "int", nullable: false),
                    KeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LockKey_BuildingUnit_BuildingUnitId",
                        column: x => x.BuildingUnitId,
                        principalTable: "BuildingUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_BuildingUnitId",
                table: "LockKey",
                column: "BuildingUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_BuildingUnit_BuildingUnitId",
                table: "Reservation",
                column: "BuildingUnitId",
                principalTable: "BuildingUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationCode_Reservation_ReservationId",
                table: "ReservationCode",
                column: "ReservationId",
                principalTable: "Reservation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_BuildingUnit_BuildingUnitId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationCode_Reservation_ReservationId",
                table: "ReservationCode");

            migrationBuilder.DropTable(
                name: "LockKey");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_BuildingUnit_BuildingUnitId",
                table: "Reservation",
                column: "BuildingUnitId",
                principalTable: "BuildingUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationCode_Reservation_ReservationId",
                table: "ReservationCode",
                column: "ReservationId",
                principalTable: "Reservation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
