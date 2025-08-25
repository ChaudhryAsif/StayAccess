using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.Latch
{
    public class EditDuringReservationRequest : LatchRequest
    {
        public DateTimeOffset EndTime { get; set; }
        public IEnumerable<string> KeyIds { get; set; }

        public EditDuringReservationRequest( DateTimeOffset endTime, IEnumerable<string> keyIds) 
        {
            EndTime = endTime;
            KeyIds = keyIds;
        }
    }
}
