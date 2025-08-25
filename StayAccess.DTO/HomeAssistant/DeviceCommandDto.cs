using System;

namespace StayAccess.DTO.HomeAssistant
{
    public class CommandDto
    {
        public string action { get; set; }
        public string unit { get; set; }
        public string code { get; set; }
        public DateTime from { get; set; }
        public DateTime to { get; set; }
        public string slot { get; set; }
        public string reservation { get; set; }
        public string newUnit { get; set; }
        public string newReservation { get; set; }
        public int codeTransactionId { get; set; }
    }
}
