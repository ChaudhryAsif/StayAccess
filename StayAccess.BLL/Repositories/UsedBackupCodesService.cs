using Microsoft.EntityFrameworkCore;
using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Reservations;
using StayAccess.Tools;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Repositories
{
    public class UsedBackupCodesService : IUsedBackupCodesService
    {

        private readonly StayAccessDbContext _stayAccessDbContext;
        private readonly IHomeAssistantService _homeAssistantService;
        private readonly IEmailService _emailService;
        private readonly ILogService _logService;

        public UsedBackupCodesService(StayAccessDbContext stayAccessDbContext,
            IHomeAssistantService homeAssistantService, IEmailService emailService, ILogService logService)
        {
            _stayAccessDbContext = stayAccessDbContext;
            _homeAssistantService = homeAssistantService;
            _emailService = emailService;
            _logService = logService;
        }

        public async Task<(bool isValid, Reservation reservation)> IsArchesBackupReservation(int reservationId)
        {
            try
            {
                _logService.LogMessage($"In UsedBackupCodesService IsArchesBackupReservation. " +
                                       $" reservationId: {reservationId}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);

                Reservation reservation = await _stayAccessDbContext.Reservation.Where(x => x.Id == reservationId).FirstOrDefaultAsync();
                
                _logService.LogMessage($"In UsedBackupCodesService IsArchesBackupReservation. " +
                                       $" reservation: {reservation.ToJsonString()}." +
                                       $" reservationId: {reservationId}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                if (reservation == null)
                {
                    return (false, new());
                }
                //return (isValid: reservation.Code.Trim().ToLower().StartsWith("backup"), reservation: reservation);
                return (isValid: reservation.Code.ToLower().Contains("backup"), reservation: reservation);
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in UsedBackupCodesService IsArchesBackupReservation. Exception: {ex.ToJsonString()}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        public async Task NotifyBackupMarkedAsUsedAsync(int backupReservationId, string triggeredBy)
        {
            try
            {
                _logService.LogMessage($"In UsedBackupCodesService NotifyBackupMarkedAsUsedAsync. " +
                                       $" backupReservationId: {backupReservationId}." +
                                       $" triggeredBy: {triggeredBy}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);

                (bool isValid, Reservation reservation) = await IsArchesBackupReservation(backupReservationId);
                if (isValid && reservation.Code.Trim().EndsWith('*'))
                {
                    BuildingUnit buildingUnit = _stayAccessDbContext.BuildingUnit.Where(x => x.Id == reservation.BuildingUnitId).FirstOrDefault();

                    _logService.LogMessage($"In UsedBackupCodesService NotifyBackupMarkedAsUsedAsync. " +
                                           $" buildingUnit: {buildingUnit.ToJsonString()}." +
                                           $" backupReservationId: {backupReservationId}." +
                                           $" triggeredBy: {triggeredBy}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);

                    string message = $"<h3>Arches Backup Reservation Marked As Used</h3>" +
                                     $"Reservation Id: {backupReservationId}" +
                                     $"<br/>Server: {_stayAccessDbContext.Database.GetDbConnection().DataSource}" +
                                     $"<br/>Database: {_stayAccessDbContext.Database.GetDbConnection().Database}" +
                                     $"</br>Triggered By: {triggeredBy}" +
                                     $"</br>" +
                                     $"<h3>Reservation:</h3>" +
                                     $"{_emailService.GetListAsHtmlTableString(null, reservation)}" +
                                     $"<h3>Building Unit:</h3>" +
                                     $"{_emailService.GetListAsHtmlTableString(null, buildingUnit)}";
                    await _homeAssistantService.SendEmailMessageAsync($"Arches Backup Reservation Marked As Used", message, null, backupReservationId);
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in UsedBackupCodesService NotifyBackupMarkedAsUsedAsync. Exception: {ex.ToJsonString()}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        public string ToCodeAsUsed(string oldReservationCode)
        {
            try
            {
                _logService.LogMessage($"In UsedBackupCodesService ToCodeAsUsed. " +
                                       $" oldReservationCode: {oldReservationCode}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);

                string codeToReturn = oldReservationCode.Trim().TrimEnd('*') + '*';

                _logService.LogMessage($"In UsedBackupCodesService ToCodeAsUsed. " +
                                       $" codeToReturn: {codeToReturn}." +
                                       $" oldReservationCode: {oldReservationCode}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);

                return codeToReturn;
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in UsedBackupCodesService ToCodeAsUsed. Exception: {ex.ToJsonString()}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        public ReservationRequestDto GetBackupReservationDtoToSetAsUsed(Reservation reservation)
        {
            try
            {

                _logService.LogMessage($"In UsedBackupCodesService GetBackupReservationDtoToSetAsUsed. " +
                                       $" reservation: {reservation.ToJsonString()}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);

                ReservationRequestDto reservationRequestDto = new()
                {
                    Id = reservation.Id,
                    NewCode = ToCodeAsUsed(reservation.Code),
                    BuildingUnitId = reservation.BuildingUnitId,
                    StartDate = reservation.StartDate,
                    EndDate = reservation.EndDate,
                    Cancelled = reservation.Cancelled,
                    EarlyCheckIn = reservation.EarlyCheckIn,
                    LateCheckOut = reservation.LateCheckOut,
                };

                _logService.LogMessage($"In UsedBackupCodesService GetBackupReservationDtoToSetAsUsed. " +
                                       $" reservationRequestDto: {reservationRequestDto.ToJsonString()}." +
                                       $" reservation: {reservation.ToJsonString()}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);
                return reservationRequestDto;
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in UsedBackupCodesService GetBackupReservationDtoToSetAsUsed. Exception: {ex.ToJsonString()}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }
    }
}
