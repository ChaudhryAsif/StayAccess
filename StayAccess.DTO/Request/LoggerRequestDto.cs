using StayAccess.DTO.Enums;

namespace StayAccess.DTO.Request
{
    public class LoggerRequestDto
    {
        public LogType LogTypeId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public int? ReservationId { get; set; }
    }
}