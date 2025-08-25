using StayAccess.DTO.Request.MC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Responses.MC
{
    public class MCApiResponse : MCReservationRequest
    {
        public int id { get; set; }
        public int? status { get; set; }
        public int? id_room { get; set; }
    }
}
