using System;
using System.ComponentModel.DataAnnotations;

namespace StayAccess.DAL.DomainEntities
{
    public class UnitSlotLog
    {
        [Key]
        public int Id { get; set; }
        public int NodeId { get; set; }
        public string Unit { get; set; }
        public string Slot { get; set; }
        public string Code { get; set; }
        public string LastUpdate { get; set; }
        public DateTime LastUpdatedTime { get; set; }
    }
}
