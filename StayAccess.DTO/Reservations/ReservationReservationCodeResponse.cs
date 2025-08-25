using StayAccess.DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Reservations
{
    public class ReservationReservationCodeResponse
    {
        public int ReservationCodeId { get; set; }
        public string LockCode { get; set; }
        public int SlotNo { get; set; }
        public CodeStatus Status { get; set; }
    }
}
