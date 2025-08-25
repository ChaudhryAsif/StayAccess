using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class new_changes_in_unitLog_and_unitLogSlot_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UnitSlotLog");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "UnitSlotLog");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "UnitSlotLog");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "UnitSlotLog");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UnitLog");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "UnitLog");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "UnitLog");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "UnitLog");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UnitSlotLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "UnitSlotLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "UnitSlotLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "UnitSlotLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UnitLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "UnitLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "UnitLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "UnitLog",
                type: "datetime2",
                nullable: true);
        }
    }
}
