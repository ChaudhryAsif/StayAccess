using StayAccess.DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request
{
    public class BuildingRequestDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public BuildingLockSystem BuildingLockSystem { get; set; }
    }
}
