using System;

namespace StayAccess.DTO.Reservations
{
    public class ReservationRequestDto
    {
        public int Id { get; set; }
        public int BuildingUnitId { get; set; }
        public string Code { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? EarlyCheckIn { get; set; }
        public DateTime? LateCheckOut { get; set; }
        public bool Cancelled { get; set; }
        public int NewBuildingUnitId { get; set; }
        public string NewCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }    
        public string Email { get; set; }
        public string Phone { get; set; }
        public int GetBuildingUnitId()
        {
            return NewBuildingUnitId > 0 ? NewBuildingUnitId : BuildingUnitId;
        }

        public string GetCode()
        {
            return !string.IsNullOrWhiteSpace(NewCode) ? NewCode : Code;
        }
    }
}
