namespace StayAccess.DTO.Reservations
{
    public class ReservationCodeDto
    {
        public int Id { get; set; }

        public int ReservationId { get; set; }

        public string LockCode { get; set; }
    }
}
