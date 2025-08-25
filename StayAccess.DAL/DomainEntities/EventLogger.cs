using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.DomainEntities
{
    public  class EventLogger
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public int NodeId { get; set; }
        public int EventId { get; set; }
        public string EventLabel { get; set; }
        public int Type { get; set; }
        public DateTime TimeFired { get; set; }
        public string EventUserId { get; set; } 

    }
}
