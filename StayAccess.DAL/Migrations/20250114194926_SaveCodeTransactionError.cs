using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SaveCodeTransactionError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Error",
                table: "CodeTransaction",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "CodeTransaction",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Error",
                table: "CodeTransaction");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "CodeTransaction");
        }
    }
}
