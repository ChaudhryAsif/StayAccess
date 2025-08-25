using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayAccess.DAL.DomainEntities
{
    public class BuildingUnit : BaseEntity
    {
        [ForeignKey("Building")]
        [Required]
        public int BuildingId { get; set; }

        /* original 
         may need it also --- the old BuildingUnitName() may have changed as it is based on this BuildingId
        public string BuildingId { get; set; }
        */

        public string UnitId { get; set; }

        public virtual Building Building { get; set; }

        public IList<Reservation> Reservations { get; } = new List<Reservation>();
        public IList<LockKey> LockKeys { get; } = new List<LockKey>();
    }
}