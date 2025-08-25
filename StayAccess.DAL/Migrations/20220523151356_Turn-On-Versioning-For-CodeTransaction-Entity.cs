using Microsoft.EntityFrameworkCore.Migrations;
using StayAccess.DAL.Extensions;

namespace StayAccess.DAL.Migrations
{
    public partial class TurnOnVersioningForCodeTransactionEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddTemporalTable("CodeTransaction");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoveTemporalTable("CodeTransaction");
        }
    }
}
