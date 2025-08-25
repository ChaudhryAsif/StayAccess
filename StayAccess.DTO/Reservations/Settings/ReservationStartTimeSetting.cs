using StayAccess.DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Reservations.Settings
{
    public class ReservationStartTimeSetting
    {
        public string Default { get; set; }
        public List<ReservationStartTimeByBuildingLockSystem> BuildingLockSystemStartTime { get; set; }

        public class ReservationStartTimeByBuildingLockSystem
        {
            public string BuildingLockSystem { get; set; }
            public string StartTime { get; set; }
        }
    }
}
