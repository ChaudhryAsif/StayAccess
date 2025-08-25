using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayAccess.DAL.Migrations
{
    /// <inheritdoc />
    public partial class changeUserUIdName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserUUid",
                table: "LockKey");

            migrationBuilder.AddColumn<Guid>(
                name: "UUid",
                table: "LockKey",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UUid",
                table: "LockKey");

            migrationBuilder.AddColumn<string>(
                name: "UserUUid",
                table: "LockKey",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
