using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Doors.ArchesFrontDoor;
using StayAccess.DTO.Enums;
using StayAccess.DTO.HomeAssistant;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace StayAccess.Arches.Interfaces
{
    public interface IHomeAssistantService
    {
        HttpStatusCode CreateDeleteUnitCode(string action, string unit, Reservation reservation, ReservationCode reservationCode, int codeTransactionId, DateTime currentEstTime, bool isCronJob = false);

        void SetCodesForUnit(Reservation reservation, List<ReservationCode> reservationCodes, bool isCronJob = false, Reservation oldReservation = null);

        void DeleteCodesForUnit(Reservation reservation, List<ReservationCode> reservationCodes, bool isCronJob = false, bool deleteFrontDoorCodes = false);

        Task<bool> ExecuteCommandsForUnitAsync(CommandDto deviceCommands, int? reservationId);
        void ExecuteCommandsForUnitFromListDto(List<MissingUnitCodeDto> missingUnitCodesList);

        Task<HttpStatusCode> CreateCodeForFrontDoorAsync(Reservation reservation, ReservationCode reservationCode, int codeTransactionId, bool isCronJob, DateTime currentEstTime);

        Task<ExistingUserResponseFrontDoorDto> GetExistingUserFrontDoorAsync(string buildingUnitId, int reservationId, string reservationCode);

        Task<bool> DeleteArchesBuildingUnitCodeAsync(int unitSlotLogId);

        Task<HttpStatusCode> DeleteCodeForFrontDoorAsync(Reservation oldReservation, bool isCronJob, DateTime currentEstTime);

        Task<HttpStatusCode> ModifiedCodeForFrontDoorAsync(Reservation reservation, bool isCronJob = false);

        Task<UpdatePanelResponseFrontDoorDto> UpdatePannelFrontDoorAsync();

        // Task ExecuteActiveCodesForUnitAsync();

        Task ExecuteFailedCodesForUnitAsync();

        Task ExecuteVerificationCodesAsync();

        Task ExecuteLowBatteryLevelEmailNotificationAsync();

        Task SendEmailMessageAsync(string subject, string message, List<string> recipients = null, int? reservationId = null);
        void AdjustCommmandDtoDatesTmeOfDay(CommandDto commandDto, bool isCronJob = false);
        Task DeleteExpiredFrontDoorCodes();
    }
}
