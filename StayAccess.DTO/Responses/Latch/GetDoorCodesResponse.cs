using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Responses.Latch
{
    public class GetDoorCodesResponse : LatchResponse< List<GetDoorCodesMessage> >, ILatchResponse
    {
    }

    public class GetDoorCodesMessage
    {
        public string DoorCode { get; set; }
        public string LockName { get; set; }
        public string LockId { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string KeyMembershipId { get; set; }
        public string PasscodeType { get; set; }
    }
}
