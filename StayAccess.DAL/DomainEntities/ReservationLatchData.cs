using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.DomainEntities
{
    public class ReservationLatchData : BaseEntity
    {
        [ForeignKey("Reservation")]
        public int ReservationId { get; set; }

        public string BuildingCode { get; set; }
        public string UnitCode { get; set; }

        public DateTime? StartDateLatch { get; set; }
        public DateTime? EndDateLatch { get; set; }

        [Required]
        public string UserUUid { get; set; }
       // public bool  IsActive { get; set; }
        public Reservation Reservation { get; set; }
    }
}
