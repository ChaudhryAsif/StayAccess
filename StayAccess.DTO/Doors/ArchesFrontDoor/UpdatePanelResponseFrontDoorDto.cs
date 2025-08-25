using System.Collections.Generic;

namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class UpdatePanelResponseFrontDoorDto
    {
        public object ResponseStatus { get; set; }
        public IEnumerable<string> MacAddresses { get; set; }
        public object[] FailedPanels { get; set; }
    }
}
