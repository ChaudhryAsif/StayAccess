using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StayAccess.DTO.Doors.LatchDoor
{
    public class LatchApi
    {
        public BaseUrl BaseUrls { get; set; }
        public LatchApiEndpoints LatchEndpoints { get; set; }
        public LatchToken LatchApiToken { get; set; }
        public LatchApiJwt LatchJwt { get; set; }

        public class BaseUrl
        {
            public string Reservation { get; set; }
           // public string Integration { get; set; }
           // public string LatchLink { get; set; }
        }
        public class LatchApiEndpoints
        {
            public string Auth { get; set; }
            public string Delete { get; set; }
          //  public string Reservation { get; set; }
            //public string DoorCodes { get; set; }
        }

        public class LatchApiJwt
        {
            public string client_id { get; set; }
            public string client_secret { get; set; }
            public string audience { get; set; }
            public string grant_type { get; set; }
        }
        public class LatchToken
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }
    }
}
