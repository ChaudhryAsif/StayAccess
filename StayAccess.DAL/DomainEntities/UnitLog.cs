using System;
using System.ComponentModel.DataAnnotations;

namespace StayAccess.DAL.DomainEntities
{
    public class UnitLog
    {
        [Key]
        public int Id { get; set; }
        public int NodeId { get; set; }
        public string Unit { get; set; }
        public string BatteryLevel { get; set; }
        public string LastUpdated { get; set; }
        public DateTime LastUpdatedTime { get; set; }
        public string HomeId { get; set; }
    }
}
