using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.MC
{
    public class MCReservationRequest
    {
        
        public string fname { get; set; }
        public string lname { get; set; }
        public string check_in { get; set; }
        public string check_out { get; set; }
        public string room_name { get; set; }
    }
}
