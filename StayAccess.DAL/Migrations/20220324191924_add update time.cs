using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class addupdatetime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedTime",
                table: "UnitLog",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedTime",
                table: "UnitLog");
        }
    }
}
