using StayAccess.DTO.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayAccess.DAL.DomainEntities
{
    public class ReservationCode : BaseEntity
    {
        [ForeignKey("Reservation")]
        public int ReservationId { get; set; }

        [Required]
        public string LockCode { get; set; }

        public int SlotNo { get; set; }

        public CodeStatus Status { get; set; }

        public virtual Reservation Reservation { get; set; }
    }
}