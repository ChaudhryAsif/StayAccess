using System.Collections.Generic;

namespace StayAccess.DTO.Reservations
{
    public class ReservationWithCodesDto : ReservationRequestDto
    {
        public ReservationWithCodesDto()
        {
            ReservationCodes = new List<ReservationCodeDto>();
        }

        public List<ReservationCodeDto> ReservationCodes { get; set; }
    }
}
