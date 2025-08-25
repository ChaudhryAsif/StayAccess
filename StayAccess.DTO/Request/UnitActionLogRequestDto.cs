using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request
{
    public class UnitActionLogRequestDto
    {
        public int InstallationId { get; set; }
        public int NodeId { get; set; }
        //  figure it out based on oterh info: public string Unit { get; set; }
        public DateTime DateTime { get; set; }
        public string EventCode { get; set; }
        public string Slot { get; set; }
    }
}
