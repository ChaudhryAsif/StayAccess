using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.Latch
{
    public class LockKeyRequestDto
    {
        public int Id { get; set; }
        public int? BuildingUnitId { get; set; }
        public int? BuildingId { get; set; }
        public string Name { get; set; }
        public Guid KeyId { get; set; }
    }
}
