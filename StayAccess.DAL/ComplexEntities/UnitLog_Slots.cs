using StayAccess.DAL.DomainEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.ComplexEntities
{
    public class UnitLog_Slots
    {
        public UnitLog UnitLog { get; set; }
        public List<UnitSlotLog> UnitSlotLogs { get; set; }
    }
}
