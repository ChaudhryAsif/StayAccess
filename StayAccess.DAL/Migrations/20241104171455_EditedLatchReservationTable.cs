using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class EditedLatchReservationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LatchReservationToken",
                table: "ReservationLatchData",
                newName: "UserUUid");

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
                name: "PasscodeType",
                table: "ReservationLatchData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
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

            migrationBuilder.CreateTable(
                name: "LatchAccessToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Expires = table.Column<int>(type: "int", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatchAccessToken", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LatchAccessToken");

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
                name: "PasscodeType",
                table: "ReservationLatchData");

            migrationBuilder.DropColumn(
                name: "Phone",
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

            migrationBuilder.RenameColumn(
                name: "UserUUid",
                table: "ReservationLatchData",
                newName: "LatchReservationToken");
        }
    }
}
