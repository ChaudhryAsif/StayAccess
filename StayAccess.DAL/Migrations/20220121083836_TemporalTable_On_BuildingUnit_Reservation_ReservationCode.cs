using Microsoft.EntityFrameworkCore.Migrations;
using StayAccess.DAL.Extensions;

namespace StayAccess.DAL.Migrations
{
    public partial class TemporalTable_On_BuildingUnit_Reservation_ReservationCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddTemporalTable("BuildingUnit");
            migrationBuilder.AddTemporalTable("Reservation");
            migrationBuilder.AddTemporalTable("ReservationCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("BuildingUnit");
            migrationBuilder.Sql("Reservation");
            migrationBuilder.Sql("ReservationCode");
        }
    }
}
