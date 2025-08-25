
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Responses.Latch
{
    public class ServerErrorResponse : LatchResponse<ServerErrorMessage>, ILatchResponse
    {
    }



    public class ServerErrorMessage 
    {
        public string error { get; set; }
        public string message { get; set; }
        public DateTime timestamp { get; set; }
        public int status { get; set; }
        public string path { get; set; }

    }




}
