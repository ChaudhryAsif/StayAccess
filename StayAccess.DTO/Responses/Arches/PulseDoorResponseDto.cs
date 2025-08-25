using System.Collections.Generic;

namespace StayAccess.DTO.Responses.Arches
{
    public class PulseDoorResponseDto
    {
        public object ResponseStatus { get; set; }

        public Dictionary<string, List<int>> DoorsByPanel { get; set; }
    }
}
