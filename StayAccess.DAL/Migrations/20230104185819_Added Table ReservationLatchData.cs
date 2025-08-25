using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class AddedTableReservationLatchData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatchEndDate",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "LatchReservationToken",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "LatchStartDate",
                table: "Reservation");

            migrationBuilder.CreateTable(
                name: "ReservationLatchData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    BuildingCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDateLatch = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateLatch = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LatchReservationToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationLatchData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationLatchData_Reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationLatchData_ReservationId",
                table: "ReservationLatchData",
                column: "ReservationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationLatchData");

            migrationBuilder.AddColumn<DateTime>(
                name: "LatchEndDate",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LatchReservationToken",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LatchStartDate",
                table: "Reservation",
                type: "datetime2",
                nullable: true);
        }
    }
}
