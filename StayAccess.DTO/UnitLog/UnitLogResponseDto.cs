using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.UnitLog
{
    public class UnitLogResponseDto
    {
        public int UnitLogId { get; set; }
        public int UnitSlotLogId { get; set; }
        public int BatteryLevel { get; set; }
        public int Slot { get; set; }
        public string Unit { get; set; }
        public int Code { get; set; }
    }
}
