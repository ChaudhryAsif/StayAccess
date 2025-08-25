using System;

namespace StayAccess.DTO.Reservations
{
    public class ReservationResponseDto
    {
        public int Id { get; set; }
        public int BuildingUnitId { get; set; }
        public string BuildingUnitUnitId { get; set; }
        public int BuildingId { get; set; }
        public string BuildingName { get; set; }
        public string Code { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EarlyCheckIn { get; set; }
        public DateTime? LateCheckOut { get; set; }
        public bool Cancelled { get; set; }
        public bool Active { get; set; }
        public ReservationReservationCodeResponse ReservationReservationCodeResponse { get; set; }
    }
}
