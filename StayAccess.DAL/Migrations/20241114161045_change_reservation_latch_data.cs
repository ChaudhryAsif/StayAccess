using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class change_reservation_latch_data : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasscodeType",
                table: "ReservationLatchData");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "ReservationLatchData");

            migrationBuilder.DropColumn(
                name: "Shareable",
                table: "ReservationLatchData");

            migrationBuilder.DropColumn(
                name: "ShouldNotify",
                table: "ReservationLatchData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasscodeType",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Shareable",
                table: "ReservationLatchData",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldNotify",
                table: "ReservationLatchData",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
