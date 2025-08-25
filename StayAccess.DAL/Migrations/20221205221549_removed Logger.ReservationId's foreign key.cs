using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class removedLoggerReservationIdsforeignkey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logger_Reservation_ReservationId",
                table: "Logger");

            migrationBuilder.DropIndex(
                name: "IX_Logger_ReservationId",
                table: "Logger");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Logger_ReservationId",
                table: "Logger",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Logger_Reservation_ReservationId",
                table: "Logger",
                column: "ReservationId",
                principalTable: "Reservation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
