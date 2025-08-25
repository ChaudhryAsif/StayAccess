using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class Buildingtableadded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BuildingId",
                table: "BuildingUnit",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Building",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildingLockSystem = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Building", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingUnit_BuildingId",
                table: "BuildingUnit",
                column: "BuildingId");

            migrationBuilder.AddForeignKey(
                name: "FK_BuildingUnit_Building_BuildingId",
                table: "BuildingUnit",
                column: "BuildingId",
                principalTable: "Building",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BuildingUnit_Building_BuildingId",
                table: "BuildingUnit");

            migrationBuilder.DropTable(
                name: "Building");

            migrationBuilder.DropIndex(
                name: "IX_BuildingUnit_BuildingId",
                table: "BuildingUnit");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "BuildingUnit",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
