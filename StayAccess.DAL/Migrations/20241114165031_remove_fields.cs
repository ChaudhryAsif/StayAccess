using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class remove_fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "ReservationLatchData");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "ReservationLatchData");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "ReservationLatchData");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ReservationLatchData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
