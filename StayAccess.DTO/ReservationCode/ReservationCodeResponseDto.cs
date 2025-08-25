using System.Collections.Generic;

namespace StayAccess.DTO.ReservationCode
{
    public class ReservationCodeResponseDto
    {
        public ReservationCodeResponseDto()
        {
            ReservationCodeIds = new List<int>();
        }

        public int ReservationId { get; set; }

        public List<int> ReservationCodeIds { get; set; }
    }
}
