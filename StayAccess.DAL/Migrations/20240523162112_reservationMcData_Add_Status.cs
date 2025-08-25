using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class reservationMcData_Add_Status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservationStatus",
                table: "ReservationMCData",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservationStatus",
                table: "ReservationMCData");
        }
    }
}
