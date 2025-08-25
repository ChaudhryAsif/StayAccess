using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.Latch
{
    public class UpdateReservationRequest : LatchRequest
    {
        public string UserUUid { get; set; }
        public string DoorUUid { get; set; }
        public bool Shareable { get; set; }
        public string EndTime { get; set; }

        public UpdateReservationRequest(string userUUId, string doorUUid, string endTime)
        {
            UserUUid = userUUId;
            DoorUUid = doorUUid;
            Shareable = false;
            EndTime = endTime;
        }
    }
}
