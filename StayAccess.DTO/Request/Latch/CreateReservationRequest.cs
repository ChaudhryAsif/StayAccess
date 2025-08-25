using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Request.Latch
{
    public class CreateReservationRequest: LatchRequest
    {
       
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public bool Shareable { get; set; }
        public string PasscodeType { get;set; }
        public string Role { get; set; }
        public bool ShouldNotify { get; set; }
        public IEnumerable<string> KeyIds { get; set; }
        //  public int AllowedKeyCardCount { get; set; } = 0;

        public CreateReservationRequest(DateTimeOffset startTime, DateTimeOffset endTime,  string firstName, string lastName, string email, string phone, List<string> doorIds)
        {
            StartTime = startTime;
            EndTime = endTime;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Phone = phone;
            KeyIds = doorIds;
            Shareable = false;
            PasscodeType = "PERMANENT";
            Role = "NON_RESIDENT";
            ShouldNotify = true;
            // AllowedKeyCardCount = allowedKeyCardCount;
        }
    }
}
