using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class addedReservationLatchStartDateLockKeyBuildingIdBuildingMriBuildingIddbconstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LockKey_BuildingUnit_BuildingUnitId",
                table: "LockKey");

            migrationBuilder.AddColumn<DateTime>(
                name: "LatchStartDate",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BuildingUnitId",
                table: "LockKey",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                table: "LockKey",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Building",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "MriBuildingId",
                table: "Building",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_BuildingId",
                table: "LockKey",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_KeyId_BuildingId",
                table: "LockKey",
                columns: new[] { "KeyId", "BuildingId" },
                unique: true,
                filter: "[BuildingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_KeyId_BuildingUnitId",
                table: "LockKey",
                columns: new[] { "KeyId", "BuildingUnitId" },
                unique: true,
                filter: "[BuildingUnitId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LockKey_BuildingUnitId_or_BuildingId_Is_Null_And_Not_Both_Null",
                table: "LockKey",
                sql: "([BuildingUnitId] IS NULL OR [BuildingId] IS NULL) AND NOT ([BuildingUnitId] IS NULL AND [BuildingId] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Building_MriBuildingId",
                table: "Building",
                column: "MriBuildingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Building_Name",
                table: "Building",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LockKey_Building_BuildingId",
                table: "LockKey",
                column: "BuildingId",
                principalTable: "Building",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LockKey_BuildingUnit_BuildingUnitId",
                table: "LockKey",
                column: "BuildingUnitId",
                principalTable: "BuildingUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LockKey_Building_BuildingId",
                table: "LockKey");

            migrationBuilder.DropForeignKey(
                name: "FK_LockKey_BuildingUnit_BuildingUnitId",
                table: "LockKey");

            migrationBuilder.DropIndex(
                name: "IX_LockKey_BuildingId",
                table: "LockKey");

            migrationBuilder.DropIndex(
                name: "IX_LockKey_KeyId_BuildingId",
                table: "LockKey");

            migrationBuilder.DropIndex(
                name: "IX_LockKey_KeyId_BuildingUnitId",
                table: "LockKey");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LockKey_BuildingUnitId_or_BuildingId_Is_Null_And_Not_Both_Null",
                table: "LockKey");

            migrationBuilder.DropIndex(
                name: "IX_Building_MriBuildingId",
                table: "Building");

            migrationBuilder.DropIndex(
                name: "IX_Building_Name",
                table: "Building");

            migrationBuilder.DropColumn(
                name: "LatchStartDate",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "LockKey");

            migrationBuilder.DropColumn(
                name: "MriBuildingId",
                table: "Building");

            migrationBuilder.AlterColumn<int>(
                name: "BuildingUnitId",
                table: "LockKey",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Building",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_LockKey_BuildingUnit_BuildingUnitId",
                table: "LockKey",
                column: "BuildingUnitId",
                principalTable: "BuildingUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
