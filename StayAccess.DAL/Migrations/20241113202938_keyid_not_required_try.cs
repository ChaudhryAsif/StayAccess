using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class keyid_not_required_try : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LockKey_KeyId_BuildingId",
                table: "LockKey");

            migrationBuilder.DropIndex(
                name: "IX_LockKey_KeyId_BuildingUnitId",
                table: "LockKey");

            migrationBuilder.AlterColumn<string>(
                name: "UUid",
                table: "LockKey",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

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
        }
    }
}
