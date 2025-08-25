using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.Latch
{
    public class EditReservationRequest : LatchRequest
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public IEnumerable<string> KeyIds { get; set; }

        public EditReservationRequest( DateTimeOffset startTime, DateTimeOffset endTime, IEnumerable<string> keyIds)
        {
           
            StartTime = startTime;
            EndTime = endTime;
            KeyIds = keyIds;
        }
    }
}
