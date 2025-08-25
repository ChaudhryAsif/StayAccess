using System.Collections.Generic;

namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    // use for modifiying front door
    public class ModifyCodeRequestFrontDoorDto
    {
        public List<Properties> Properties { get; set; }
    }

    // use for modifiying front door
    public class Properties
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
