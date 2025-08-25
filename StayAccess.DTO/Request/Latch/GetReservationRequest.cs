using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.Latch
{
    public class GetReservationRequest : LatchRequest
    {
        public string UserUUid { get; set; }

        public GetReservationRequest(string userUUid) 
        {
           UserUUid = userUUid;
        }
    }
}
