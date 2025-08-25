using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class editReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ReservationLatchData");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Reservation");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
