
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Responses.Latch
{
    public class GetReservationResponse : LatchResponse<GetReservationMessage>, ILatchResponse
    {
    }



    public class GetReservationMessage : ILatchMessage
    {
      
        public int startTime { get; set; }
        public int endTime { get; set; }
        public List<Guid> keyIds { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public int allowedKeycardCount { get; set; }
        public int availableKeycardCount { get; set; }
        public bool isCancelled { get; set; }
        public bool isInternetConnected { get; set; }
    }




}
