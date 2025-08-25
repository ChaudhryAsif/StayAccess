using StayAccess.DTO.Enums;
using System;

namespace StayAccess.DTO.Request
{
    public class EventLoggerRequestDto
    {
        public string DeviceId { get; set; }
        public int NodeId { get; set; }
        public int EventId { get; set; }
        public string EventLabel { get; set; }
        public int Type { get; set; }
        public DateTime TimeFired { get; set; }
        public string UserId { get; set; }
    }
}