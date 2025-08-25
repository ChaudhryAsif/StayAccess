using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Reservations
{
    public class CurrentReservationResponseDto
    {
        public int ReservationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? EarlyCheckIn { get; set; }
        public DateTime? LateCheckOut { get; set; }
        public int? FrontDoorUserId { get; set; }
        public string BuildingUnit { get; set; }
        public int Slot { get; set; }
        public string LockCode { get; set; }

    }
}
