using System;

namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class VerifyFrontDoorCodeDto
    {
        public DateTime? ExpiresOn { get; set; }

        public string PinNumber { get; set; }

        public int ReservationId { get; set; }

        public string Unit { get; set; }

        public string LockCode { get; set; }

        public int SlotNo { get; set; }

        public DateTime EndDate { get; set; }

        public string ReservationCode { get; set; }
    }
}
