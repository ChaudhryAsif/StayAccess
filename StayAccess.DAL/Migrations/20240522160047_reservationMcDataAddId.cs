using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class reservationMcDataAddId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "McId",
                table: "ReservationMCData",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "McIdRoom",
                table: "ReservationMCData",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "McId",
                table: "ReservationMCData");

            migrationBuilder.DropColumn(
                name: "McIdRoom",
                table: "ReservationMCData");
        }
    }
}
