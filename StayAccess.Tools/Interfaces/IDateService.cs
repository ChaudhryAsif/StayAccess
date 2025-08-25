using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Interfaces
{
    public interface IDateService
    {
        public DateTime GetToDate(Reservation reservation, bool isCronJob, DateTime currentEstTime, bool returnLatestToDate);
        public DateTime GetToDate(DateTime toDate, int? reservationId, bool isCronJob, DateTime currentEstTime, bool returnLatestToDate);
        public DateTime GetFromDate(Reservation reservation, BuildingLockSystem buildingLockSystem, int? reservationId, DateTime currentEstTime, bool isCronJob, bool returnEarliestFromDate);
        public DateTime GetFromDate(DateTime fromDate, BuildingLockSystem buildingLockSystem, int? reservationId, DateTime currentEstTime, bool isCronJob, bool returnEarliestFromDate);
        public string GetReservationStartTimeSetting(BuildingLockSystem buildingLockSystem, int? reservationId, DateTime currentEstTime, bool isCronJob);
    }
}
