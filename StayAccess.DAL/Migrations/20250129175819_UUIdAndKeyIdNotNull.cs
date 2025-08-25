using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UUIdAndKeyIdNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LockKey_UUid_BuildingId",
                table: "LockKey");

            migrationBuilder.DropIndex(
                name: "IX_LockKey_UUid_BuildingUnitId",
                table: "LockKey");

            migrationBuilder.AlterColumn<string>(
                name: "UUid",
                table: "LockKey",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "KeyId",
                table: "LockKey",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_UUid_BuildingId",
                table: "LockKey",
                columns: new[] { "UUid", "BuildingId" },
                unique: true,
                filter: "[UUid] IS NOT NULL AND [BuildingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_UUid_BuildingUnitId",
                table: "LockKey",
                columns: new[] { "UUid", "BuildingUnitId" },
                unique: true,
                filter: "[UUid] IS NOT NULL AND [BuildingUnitId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LockKey_UUid_BuildingId",
                table: "LockKey");

            migrationBuilder.DropIndex(
                name: "IX_LockKey_UUid_BuildingUnitId",
                table: "LockKey");

            migrationBuilder.AlterColumn<string>(
                name: "UUid",
                table: "LockKey",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "KeyId",
                table: "LockKey",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_UUid_BuildingId",
                table: "LockKey",
                columns: new[] { "UUid", "BuildingId" },
                unique: true,
                filter: "[BuildingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LockKey_UUid_BuildingUnitId",
                table: "LockKey",
                columns: new[] { "UUid", "BuildingUnitId" },
                unique: true,
                filter: "[BuildingUnitId] IS NOT NULL");
        }
    }
}
