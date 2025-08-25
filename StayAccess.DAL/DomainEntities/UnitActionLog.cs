using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.DomainEntities
{
    public class UnitActionLog
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public string InstallationId { get; set; }
        [Required]
        public int NodeId { get; set; }
        [Required]
        public string Unit { get; set; }
        [Required]
        public DateTime DateTime { get; set; }
        [Required]
        public string EventCode { get; set; }
        public string Slot { get; set; }//not required
    }
}
