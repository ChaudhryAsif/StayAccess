using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Doors.ArchesFrontDoor;
using StayAccess.DTO.HomeAssistant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Latch.Interfaces
{
    public interface ILatchService
    {
        // Task CreateRemoveLatchReservationAsync(Reservation reservation, ReservationCode reservationCode, DateTime currentEstTime, bool isCronJob = false); // => throw new NotImplementedException();

        Task<HttpStatusCode> CreateLatchReservationAsync(Reservation reservation, ReservationLatchData reservationLatchData, ReservationCode reservationCode, bool isCronJob, DateTime currentEstTime);

        Task<HttpStatusCode> RemoveLatchReservationAsync(Reservation reservation, ReservationLatchData reservationLatchData, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime, string oldUnitId = null);

        Task<HttpStatusCode> UpdateLatchReservationAsync(Reservation reservation, ReservationLatchData reservationLatchData, string oldUnit, bool unitChange, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime);
        Task <(bool reservationStarted, bool reservationStartedAndSetToFuture)> CheckIfLatchReservationStartedAndReservationStartChangedToTheFutureAsync(Reservation reservation, ReservationLatchData reservationLatchData, DateTime newStartDate, DateTime currentEstTime, bool isCronJob);






        //Task<ExistingUserResponseFrontDoorDto> GetExistingUserAmenityAsync(string buildingUnitId, string reservationCode);
        //void DeleteCodesForLatch(Reservation reservation, List<ReservationCode> reservationCodes, bool isCronJob = false);
        //Task<bool> DeleteBuildingDoorCodeAsync(int unitSlotLogId);
        //Task<bool> DeleteLatchBuildingUnitCodeAsync(int unitSlotLogId);
        //Task DeleteCodeForAmenityAsync(Reservation oldReservation, bool isCronJob, DateTime currentEstTime);
        //Task ModifiedCodeForAmenityAsync(Reservation reservation);
        //void SetCodesForDoor(Reservation reservation, List<ReservationCode> reservationCodes, bool isCronJob = false, Reservation oldReservation = null);
        //void DeleteCodesForDoor(Reservation reservation, List<ReservationCode> reservationCodes, bool isCronJob = false, bool deleteFrontDoorCodes = false);
        //Task<bool> ExecuteCommandsForDoorAsync(CommandDto deviceCommands);
        //Task CreateCodeForAmenityAsync(Reservation reservation, ReservationCode reservationCode, bool isCronJob, DateTime currentEstTime);
        //Task<UpdatePanelResponseFrontDoorDto> UpdatePannelAmenityAsync();
        // Task ExecuteActiveCodesForDoorAsync();
        //Task ExecuteFailedCodesForDoorAsync();
        //Task ExecuteVerificationCodesAsync();
        //Task ExecuteLowBatteryLevelEmailNotificationAsync();
        //Task SendEmailMessageAsync(string subject, string message, List<string> recipients = null);
    }
}
