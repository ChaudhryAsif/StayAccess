using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class VerifyCodeRequestFrontDoor
    {
        public string BuildingUnitId { get; set; }
        public string ReservationCode { get; set; }
    }
}
