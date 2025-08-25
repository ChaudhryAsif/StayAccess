using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.DomainEntities
{
    public class LockKey : BaseEntity
    {
        [ForeignKey("BuildingUnit")]
        public int? BuildingUnitId { get; set; }

        [ForeignKey("BuildingId")]
        public int? BuildingId { get; set; }
        public Guid? KeyId { get; set; }
        public string Name { get; set; }
        public string UUid { get; set; }

        public virtual BuildingUnit BuildingUnit { get; set; }
        public virtual Building Building { get; set; }
    }
}
