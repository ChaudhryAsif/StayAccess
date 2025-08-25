using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class MissingUnitCodeDto
    {
        public int ReservationId { get; set; }
        public string ReservationCode { get; set; }
        public string Unit { get; set; }
        public string LockCode { get; set; }
        public int SlotNo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
