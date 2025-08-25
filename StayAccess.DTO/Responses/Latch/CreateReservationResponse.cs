using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Responses.Latch
{
    public class CreateReservationResponse : LatchResponse<CreateReservationMessage>, ILatchResponse
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserUUid { get; set; }
        public string Phone { get; set; }
        public List<Access> Accesses { get; set; }
    }

    public class CreateReservationMessage : ILatchMessage
    {
        //public string link { get; set; }
        public string UserUUid { get; set; }
    }

    public class Access
    {
        public string DoorUUid { get; set; }
        public string PasscodeType { get; set; }
        public bool Shareable { get; set; }
        public DateTimeOffset StartTime { get; set; }//getting back type string from response
        public DateTimeOffset EndTime { get; set; }//getting back type string from response
        public Granter Granter { get; set; }
        public string Role { get; set; }
        public DoorCode DoorCode { get; set; }


    }
    public class DoorCode
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }
    public class Granter
    {
        public string Type { get; set; }
        public string UUid { get; set; }
    }
}
