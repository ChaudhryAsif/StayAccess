using StayAccess.DTO.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.DomainEntities
{
    public class Building : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public BuildingLockSystem BuildingLockSystem { get; set; }

        public IList<LockKey> LockKeys { get; } = new List<LockKey>();
        public IList<BuildingUnit> BuildingUnits { get; } = new List<BuildingUnit>();
    }
}
