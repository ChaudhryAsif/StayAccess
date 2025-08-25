using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.Latch
{
    public class CancelReservationRequest : LatchRequest
    {
        public string UserUUid { get; set; }
        public string DoorUUid { get; set; }

        public CancelReservationRequest(string userUUid, string doorUUid)
        {
            UserUUid = userUUid;
            DoorUUid = doorUUid;
        }
    }
}
