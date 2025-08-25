using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class Add_CreatedBy_ModifiedBy_CreatedDate_ModifiedDate_Fields_in_each_database_table_And_save_its_information : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ReservationCode",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ReservationCode",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ReservationCode",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ReservationCode",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "BuildingUnit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "BuildingUnit",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "BuildingUnit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "BuildingUnit",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ReservationCode");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ReservationCode");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ReservationCode");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ReservationCode");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BuildingUnit");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "BuildingUnit");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "BuildingUnit");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "BuildingUnit");
        }
    }
}
