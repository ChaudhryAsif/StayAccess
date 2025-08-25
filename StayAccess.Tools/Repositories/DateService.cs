using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Reservations.Settings;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Repositories
{
    public class DateService : IDateService
    {
        private readonly IConfiguration _configuration;
        private readonly ReservationStartTimeSetting _reservationStartTimeSetting;
        private readonly ILogService _logRepo;

        public DateService(IConfiguration configuration, IOptions<ReservationStartTimeSetting> reservationStartTimeSetting, ILogService logRepo)
        {
            _configuration = configuration;
            _reservationStartTimeSetting = reservationStartTimeSetting.Value;
            _logRepo = logRepo;
        }

        public DateTime GetToDate(Reservation reservation, bool isCronJob, DateTime currentEstTime, bool returnLatestToDate)
        {
            DateTime toDate = reservation.ToDate();
            return GetToDate(toDate, reservation.Id, isCronJob, currentEstTime, returnLatestToDate);
        }

        public DateTime GetToDate(DateTime toDate, int? reservationId, bool isCronJob, DateTime currentEstTime, bool returnLatestToDate)
        {
            string settingsEndTime = _configuration["ReservationEndTime"];
            DateTime toDateToReturn = !string.IsNullOrWhiteSpace(settingsEndTime) ? DateTimeExtension.GetDateTime(toDate, settingsEndTime) : toDate;

            if (returnLatestToDate)
                toDateToReturn = new DateTime(Math.Max(toDate.Ticks, toDateToReturn.Ticks));

            //_logRepo.LogMessage($"Getting to date for reservation." +
            //   $" toDate: {toDate}." +
            //   $" toDateToReturn: {toDateToReturn}." +
            //   $" settingsEndTime: {settingsEndTime}." +
            //   $" bool returnLatestToDate = {returnLatestToDate}.",
            //               reservationId, isCronJob, currentEstTime, LogType.Information);

            return toDateToReturn;
        }

        public DateTime GetFromDate(Reservation reservation, BuildingLockSystem buildingLockSystem, int? reservationId, DateTime currentEstTime, bool isCronJob, bool returnEarliestFromDate)
        {
            DateTime fromDate = reservation.FromDate();
            DateTime fromDateToReturn = GetFromDate(fromDate, buildingLockSystem, reservationId, currentEstTime, isCronJob, returnEarliestFromDate);

            //_logRepo.LogMessage($"Getting from date from 'reservation' ." +
            // $" fromDate: {fromDate}." +
            // $" fromDateToReturn: {fromDateToReturn}." +
            // $" _reservationStartTimeSetting: {_reservationStartTimeSetting.ToJsonString()}.",
            //                     reservationId, isCronJob, currentEstTime, LogType.Information);

            return fromDateToReturn;
        }

        public DateTime GetFromDate(DateTime fromDate, BuildingLockSystem buildingLockSystem, int? reservationId, DateTime currentEstTime, bool isCronJob, bool returnEarliestFromDate)
        {
            string settingsStartTime = GetReservationStartTimeSetting(buildingLockSystem, reservationId, currentEstTime, isCronJob);

            DateTime fromDateToReturn = !string.IsNullOrWhiteSpace(settingsStartTime) ? DateTimeExtension.GetDateTime(fromDate, settingsStartTime) : fromDate;

            if (returnEarliestFromDate)
                 fromDateToReturn = new DateTime(Math.Min(fromDate.Ticks, fromDateToReturn.Ticks));

            //_logRepo.LogMessage($"Getting from date from 'fromDate' ." +
            //    $" settingsStartTime: {settingsStartTime}." +
            //    $" fromDateToReturn: {fromDateToReturn}." +
            //    $" _reservationStartTimeSetting: {_reservationStartTimeSetting.ToJsonString()}." +
            //    $" bool earliestFromDate = {returnEarliestFromDate}.",
            //                        reservationId, isCronJob, currentEstTime, LogType.Information);

            return fromDateToReturn;
        }

        public string GetReservationStartTimeSetting(BuildingLockSystem buildingLockSystem, int? reservationId, DateTime currentEstTime, bool isCronJob)
        {
            string buildingLockSystemString = ((int)buildingLockSystem).ToString();
            string buildingLockSystemSetting = _reservationStartTimeSetting?.BuildingLockSystemStartTime?.Where(x => x.BuildingLockSystem == buildingLockSystemString)
                                                                                                                          ?.Select(x => x.StartTime)
                                                                                                                          .FirstOrDefault() ?? null;

            string startTimeToReturn = !string.IsNullOrWhiteSpace(buildingLockSystemSetting) ? buildingLockSystemSetting : _reservationStartTimeSetting.Default;

            //_logRepo.LogMessage($"Getting from application setting 'ReservationStartTimeSetting'." +
            //    $" buildingLockSystemString : {buildingLockSystemString}." +
            //    $" buildingLockSystemSetting : { buildingLockSystemSetting}." +
            //    $" startTimeToReturn : {startTimeToReturn}." +
            //    $" _reservationStartTimeSetting: {_reservationStartTimeSetting.ToJsonString()}.",
            //                           reservationId, isCronJob, currentEstTime, LogType.Information);

            return startTimeToReturn;

        }
    }
}
