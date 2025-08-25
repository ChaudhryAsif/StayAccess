using Microsoft.EntityFrameworkCore.Migrations;

namespace StayAccess.DAL.Migrations
{
    public partial class removedBuildingMriBuildingId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Building_MriBuildingId",
                table: "Building");

            migrationBuilder.DropColumn(
                name: "MriBuildingId",
                table: "Building");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MriBuildingId",
                table: "Building",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Building_MriBuildingId",
                table: "Building",
                column: "MriBuildingId",
                unique: true);
        }
    }
}
