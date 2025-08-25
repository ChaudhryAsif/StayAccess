using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.DomainEntities
{
    public class ReservationMCData : BaseEntity
    {
        [ForeignKey("Reservation")]
        public int ReservationId { get; set; }
        public int McId { get; set; }
        public int McIdRoom { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string UnitId { get; set; }

        public Reservation Reservation { get; set; }
        public ReservationStatus ReservationStatus { get; set; }
    }
    public enum ReservationStatus
    {
        Created = 1, 
        Deleted = 2
    }
}
