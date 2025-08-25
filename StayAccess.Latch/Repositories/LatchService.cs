using Azure.Core;
using CL.Common.Models;
using CL.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nancy.Json;
using Newtonsoft.Json;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO.APISettings;
using StayAccess.DTO.Doors.LatchDoor;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request.Latch;
using StayAccess.DTO.Responses.Latch;
using StayAccess.Latch.Interfaces;
using StayAccess.Tools;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using StayAccess.Tools.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
//using CL.Common.Models;
//using CL.Common.Services;

namespace StayAccess.Latch.Repositories
{
    public class LatchService : ILatchService
    {
        private readonly ILatchReservationService _latchReservationRepo;
        private readonly IDateService _dateRepo;
        private readonly IConfiguration _configuration;
        private readonly LatchApi _latchApi;
        private readonly IGenericService<Reservation> _reservationRepo;
        //private readonly StayAccessDbContext _dbContext;
        private readonly ILogService _logRepo;
        //private readonly ILoggerService<LatchService> _loggerRepo;
        //private readonly IEmailService _emailRepo;
        //private readonly IGenericService<Reservation> _reservationRepo;
        //private readonly APIProviderSettings _apiProviderSettings;
        private readonly IAPIProviderService _apiProviderRepo;
        private readonly IGenericService<ReservationCode> _reservationCodeRepo;
        private readonly ILatchIntegrationService _latchIntegrationService;
        private readonly StayAccessDbContext _stayAccessDbContext;


        public LatchService(ILatchReservationService latchReservationRepo, IDateService dateRepo,
            IConfiguration configuration, IOptions<LatchApi> latchApi, IGenericService<Reservation> reservationRepo, ILogService logRepo,
            IAPIProviderService apiProviderRepo, IGenericService<ReservationCode> reservationCodeRepo, ILatchIntegrationService latchIntegrationService, StayAccessDbContext stayAccessDbContext)
        {
            _latchReservationRepo = latchReservationRepo;
            _dateRepo = dateRepo;
            _configuration = configuration;
            _latchApi = latchApi.Value;
            //_dbContext = dbContext;
            _logRepo = logRepo;
            _reservationRepo = reservationRepo;
            //_loggerRepo = loggerRepo;
            //_emailRepo = emailRepo;
            //_apiProviderSettings = apiProviderSettings.Value;
            _apiProviderRepo = apiProviderRepo;
            _reservationCodeRepo = reservationCodeRepo;
            _latchIntegrationService = latchIntegrationService;
            _stayAccessDbContext = stayAccessDbContext;
        }

        /// <summary>
        /// create code for latch door
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="reservationCode"></param>
        /// <param name="isCronJob"></param>
        /// <param name="currentEstTime"></param>
        /// <returns></returns>
        public async Task<HttpStatusCode> CreateLatchReservationAsync(Reservation reservation, ReservationLatchData reservationLatchData, ReservationCode reservationCode, bool isCronJob, DateTime currentEstTime)
        {
            string safeReservationRequest = "";
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;

                string unitId = await GetUnitId(reservation.Id);

                List<string> keyIds = await GetKeyIds(reservation.BuildingUnitId);
                if (keyIds.Count < 1)
                {
                    _logRepo.LogMessage($"No keyIds found when attempting to create latch reservation. Reservation {reservation.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                    throw new NullReferenceException();
                }

                CreateReservationRequest createReservationRequest = new(
                                                          GetValidStartDateUTC(currentEstTime, reservation, isCronJob),
                                                          DateTimeExtension.FromESTToUTCDateTime(_dateRepo.GetToDate(reservation, isCronJob, currentEstTime, true)),
                                                          $"Latch-{unitId}",
                                                          $"{reservation.Code}",
                                                          reservation.Email,
                                                          reservation.Phone,
                                                          keyIds);

                safeReservationRequest = ToSafeRequestJson(createReservationRequest);
                _logRepo.LogMessage($"Creating latch reservation. ReservationId: {reservation.Id}. Payload: {safeReservationRequest}.", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                LatchReturn<CreateReservationResponse> response = new();
                response = await _latchReservationRepo.CreateReservationAsync(createReservationRequest, reservation.Id, isCronJob, currentEstTime);

                statusCodeToReturn = response.ReturnCode;
                bool success = await SetReservationCodeStatusGetIsSuccess(reservation, reservationCode, CodeStatusAction.Create, isCronJob, currentEstTime, response);
                if (success)
                {
                    _logRepo.LogMessage($"Created latch reservation successfully. ReservationId: {reservation.Id}. Payload: {safeReservationRequest}. Response: {response.ToJsonString()}.", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    await SaveLatchReservationAndSetCodeStatusToActive(reservation, keyIds, response.Response, isCronJob, currentEstTime);
                    await SaveLatchReservationStartDate(reservation, reservationLatchData, createReservationRequest.StartTime, isCronJob, currentEstTime);//need this?
                    await SaveLatchReservationEndDate(reservation, reservationLatchData, createReservationRequest.EndTime, isCronJob, currentEstTime);//need this?
                    await CaptureLatchCodesSaveAndThenSendToMRI(reservation, keyIds, response.Response, isCronJob, currentEstTime);
                }
                else
                {
                    _logRepo.LogMessage($"Create latch reservation failed. ReservationId: {reservation.Id}. Payload: {safeReservationRequest}. Response: {response.ToJsonString()}.", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                }
                return statusCodeToReturn;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Create latch reservation failed. Payload: {safeReservationRequest}. Response: {ex.Message}. StackTrace: {ex.StackTrace}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }



        /// <summary>
        /// modified code for latch door
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="isCronJob"></param>
        /// <param name="reservationCode"></param>
        /// <param name="currentEstTime"></param>
        /// <returns></returns>
        public async Task<HttpStatusCode> UpdateLatchReservationAsync(Reservation reservation, ReservationLatchData reservationLatchData, string oldUnit, bool unitChange, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime)
        {
            try
            {
                // if changed end date update latch reservation otherwise delete and re-create
                _logRepo.LogMessage($"Updating latch reservation through API. Reservation: {reservation.ToJsonString()}. ReservationLatchData: {reservationLatchData.ToJsonString()}. Unit Change: {unitChange}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                if (reservation.EndDate != reservationLatchData?.EndDateLatch && !unitChange && reservation.StartDate <= reservationLatchData?.StartDateLatch) 
                {
                    List<string> keyIds = await GetKeyIds(reservation.BuildingUnitId);
                    var response = await UpdateLatchReservationAsync(reservation, reservationLatchData, isCronJob, reservationCode, currentEstTime);
                    return response;
                }
                else
                {
                    if (reservationLatchData != null)
                    {
                        var cancelResponse = await RemoveLatchReservationAsync(reservation, reservationLatchData, isCronJob, reservationCode, currentEstTime, oldUnit);
                        if (!APIResponseService.IsSuccessCode(cancelResponse))
                            return cancelResponse;
                    }
                    var createResponse = await CreateLatchReservationAsync(reservation, reservationLatchData, reservationCode, isCronJob, currentEstTime);
                    return createResponse;
                }
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Error occurred while updating a latch Reservation. Reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }



        public (bool reservationEnded, bool reservationEndedAndSetToFuture) CheckIfLatchReservationEndedAndReservationEndChangedToTheFuture(Reservation reservation, ReservationLatchData reservationLatchData, DateTime newEndDate, DateTime currentEstTime, bool isCronJob)
        {
            if (reservationLatchData.EndDateLatch == null)
            {
                _logRepo.LogMessage($"ReservationLatchData.LatchEndDate is null. Assuming reservation hadn't been created yet. Reservation Id: {reservation.Id}", reservation.Id, false, currentEstTime, LogType.Information);
                return (false, false);
            }

            DateTime newEndDateWithSetting = _dateRepo.GetToDate(newEndDate, reservation.Id, isCronJob, currentEstTime, true).ToESTDateTime();

            bool reservationEnded = DateTime.Compare(
                                                    DateTimeExtension.FromESTToUTCDateTime((DateTime)reservationLatchData.EndDateLatch),
                                                    DateTimeExtension.FromESTToUTCDateTime(currentEstTime)) < 0;
            bool reservationEndDateChangedToTheFuture = DateTime.Compare(
                                                                        DateTimeExtension.FromESTToUTCDateTime(currentEstTime),
                                                                        DateTimeExtension.FromESTToUTCDateTime(newEndDateWithSetting)) < 0;

            return (reservationEnded, reservationEnded && reservationEndDateChangedToTheFuture);
        }



        public async Task<(bool reservationStarted, bool reservationStartedAndSetToFuture)> CheckIfLatchReservationStartedAndReservationStartChangedToTheFutureAsync(Reservation reservation, ReservationLatchData reservationLatchData, DateTime newStartDate, DateTime currentEstTime, bool isCronJob)
        {
            try
            {
                reservationLatchData = await _stayAccessDbContext.ReservationLatchData.Where(x => x.ReservationId == reservation.Id).SingleOrDefaultAsync();

                if (reservationLatchData == default)
                {
                    throw new Exception($"Error: Latch CheckIfLatchReservationStartedAndReservationStartChangedToTheFuture method: couldn't find ReservationLatchData record for reservation.");
                }
                else if (reservationLatchData.StartDateLatch == null)
                {
                    throw new Exception($"Error: Latch CheckIfLatchReservationStartedAndReservationStartChangedToTheFuture method: ReservationLatchData.LatchStartDate is null.");
                }
                else
                {
                    _logRepo.LogMessage($"Found ReservationLatchData record for reservation. " +
                                        $"In Latch CheckIfLatchReservationStartedAndReservationStartChangedToTheFuture method. " +
                                        $"ReservationLatchData: {reservationLatchData.ToJsonString()}. " +
                                        $"Reservation Id: {reservation.Id}", reservation.Id, false, currentEstTime, LogType.Information);
                }

                DateTime newStartDateWithSetting = _dateRepo.GetFromDate(newStartDate, BuildingLockSystem.Latch, reservation.Id, currentEstTime, isCronJob, true).ToESTDateTime();

                bool reservationStarted = DateTime.Compare(
                                                            DateTimeExtension.FromESTToUTCDateTime((DateTime)reservationLatchData.StartDateLatch),
                                                            DateTimeExtension.FromESTToUTCDateTime(currentEstTime)) < 0;
                bool reservationStartDateChangedToTheFuture = DateTime.Compare(
                                                                                DateTimeExtension.FromESTToUTCDateTime(currentEstTime),
                                                                                DateTimeExtension.FromESTToUTCDateTime(newStartDateWithSetting)) < 0;
                return (reservationStarted, reservationStarted && reservationStartDateChangedToTheFuture);
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Latch reservation error occurred in CheckIfLatchReservationStartedAndReservationStartChangedToTheFuture method. " +
                                    $"Exception: {ex.ToJsonString()}", reservation.Id, false, currentEstTime, LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// delete code for latch door
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="isCronJob"></param>
        /// <param name="currentEstTime"></param>
        /// <returns></returns>
        public async Task<HttpStatusCode> RemoveLatchReservationAsync(Reservation reservation, ReservationLatchData reservationLatchData, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime, string oldUnitId = null)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                if (reservationLatchData.StartDateLatch == default)
                    throw new Exception("ReservationLatchData.StartDateLatch not found when attempting to remove latch reservation.");

                //bool reservationStarted = DateTime.Compare(
                //                                            DateTimeExtension.FromESTToUTCDateTime((DateTime)reservationLatchData.StartDateLatch),
                //                                            DateTimeExtension.FromESTToUTCDateTime(currentEstTime)) < 0;
                //if (reservationStarted)
                //{ 
                //    //DIDN'T HANDLE FOR THIS CASE
                //    statusCodeToReturn = EditLatchReservationToExpire(reservation, reservationLatchData, reservationCode, currentEstTime, isCronJob).GetAwaiter().GetResult().ReturnCode;
                //}
                //else
                // {
                statusCodeToReturn = await CancelLatchReservation(reservation, reservationLatchData, isCronJob, reservationCode, currentEstTime, oldUnitId);
                // }


                if (APIResponseService.IsSuccessCode(statusCodeToReturn))
                {
                    _logRepo.LogMessage($"Successfully removing latch reservation. Reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    _logRepo.LogMessage($"Deleting latchReservationToken, latchStartDate, and latchEndDate from latch Reservation. After successfully removing latch reservation. Reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    try
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                        optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                        await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                        {
                            // ReservationLatchData dbReservationLatchData = dbContext.ReservationLatchData.FirstOrDefault(x => x.ReservationId == reservation.Id);

                            if (reservationLatchData != default)
                            {
                                // reservationLatchData.IsActive = false;
                                dbContext.Remove(reservationLatchData);
                                dbContext.SaveChanges();
                            }
                        };
                        _logRepo.LogMessage($"Successfully deleted latchReservationToken, latchStartDate, and latchEndDate from latch Reservation. After successfully removing latch reservation. Reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    }
                    catch (Exception ex)
                    {
                        _logRepo.LogMessage($"Failed to delete latchReservationToken, latchStartDate, and latchEndDate from latch Reservation. After successfully removing latch reservation. Reservation Id: {reservation.Id}. Error:{ex.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                        throw;
                    }
                }
                return statusCodeToReturn;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Error occurred in removing latch Reservation. Reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }

        public async Task<HttpStatusCode> UpdateLatchReservationAsync(Reservation reservation, ReservationLatchData reservationLatchData, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime, string oldUnitId = null)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                if (reservationLatchData.StartDateLatch == default)
                    throw new Exception("ReservationLatchData.StartDateLatch not found when attempting to update latch reservation.");

                statusCodeToReturn = await UpdateLatchReservation(reservation, reservationLatchData, isCronJob, reservationCode, currentEstTime, oldUnitId);

                if (APIResponseService.IsSuccessCode(statusCodeToReturn))
                {
                    _logRepo.LogMessage($"Successfully updated latch reservation. Reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                }
                return statusCodeToReturn;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Error occurred in updating latch Reservation. Reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }

        private async Task<LatchReturn<EditCancelReservationResponse>> EditLatchReservationToExpire(Reservation reservation, ReservationLatchData reservationLatchData, ReservationCode reservationCode, DateTime currentEstTime, bool isCronJob)
        {
            try
            {
                LatchReturn<EditCancelReservationResponse> latchReturn = new();

                int minToExpire = 1;
                List<string> keyIds = await GetKeyIds(reservation.BuildingUnitId);
                if (keyIds.Count < 1)
                {
                    _logRepo.LogMessage($"No keyIds found when attempting to modify latch reservation during reservation. Reservation: {reservation.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                    throw new NullReferenceException();
                }

                latchReturn = await EditAfterReservationStarted(reservation, reservationLatchData, reservationCode, keyIds, DateTime.Now.AddMinutes(minToExpire), isCronJob, true);

                return latchReturn;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //   
        //private async Task<(HttpStatusCode statusCodeToReturn, string payloadMessage)> EditAfterReservationStarted(Reservation reservation, ReservationCode reservationCode, List<Guid> keyIds, DateTime endTime, DateTime currentEstTime, bool isCronJob, bool reasonForEditIsToExpire = false)
        private async Task<LatchReturn<EditCancelReservationResponse>> EditAfterReservationStarted(Reservation reservation, ReservationLatchData reservationLatchData, ReservationCode reservationCode,
            List<string> keyIds, DateTime endTime, bool isCronJob, bool reasonForEditIsToExpire = false)
        {
            //don't send currentEst as time is of essence for this and the logs
            try
            {
                //doesn't send startTime because reservation already started. Preventing the latch api "INVALID_START_TIME" error.
                EditDuringReservationRequest editDuringReservationRequest = new(
                                                     endTime,
                                                     keyIds);

                string safeReservationRequest = ToSafeRequestJson(editDuringReservationRequest);
                //  DateTime startTimeEst = editToExpireReservationRequest.StartTime.DateTime.ToESTDateTime();
                DateTime endTimeEst = editDuringReservationRequest.EndTime.DateTime.ToESTDateTime();

                LatchReturn<EditCancelReservationResponse> result = await _latchReservationRepo.EditDuringReservationAsync(editDuringReservationRequest, reservation.Id, isCronJob, Utilities.GetCurrentTimeInEST());


                string editReason = (reasonForEditIsToExpire ? "Reason for edit: setting reservation to expire." : "");
                _logRepo.LogMessage($"Editing latch reservation  (after reservation started). {editReason} Reservation: {reservation.ToJsonString()}. Payload: {safeReservationRequest}. CurrentEstTime: {Utilities.GetCurrentTimeInEST()}. EndTimeEst: {endTimeEst}.", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                bool success = await SetReservationCodeStatusGetIsSuccess(reservation, reservationCode, reasonForEditIsToExpire ? CodeStatusAction.EditToExpire : CodeStatusAction.Edit, isCronJob, Utilities.GetCurrentTimeInEST(), result);
                if (success)
                {
                    _logRepo.LogMessage($"Edited latch reservation successfully (after reservation started). {editReason} Reservation: {reservation.ToJsonString()}. Payload: {safeReservationRequest}. Response: {result.ToJsonString()}. CurrentEstTime: {Utilities.GetCurrentTimeInEST()}. EndTimeEst: {endTimeEst}.", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information);
                    await SaveLatchReservationEndDate(reservation, reservationLatchData, editDuringReservationRequest.EndTime, isCronJob, Utilities.GetCurrentTimeInEST());

                    try
                    {
                        if (reasonForEditIsToExpire == true && result?.ReturnMessage != null && result.ReturnMessage.Contains("error: RESERVATION_EXPIRED - message: Reservation has expired"))
                        {
                            _logRepo.LogMessage($"Reservation has expired in latch api when trying to edit latch reservation to expire. " +
                                $"Setting latch response ReturnCode to OK (response ReturnCode was {result.ReturnCode}), so it won't try to set it to expire again. " +
                                $"ReservationId: {reservation.Id}. " +
                                $"Payload: {safeReservationRequest}. " +
                                $"Response: {result.ToJsonString()}. " +
                                $"CurrentEstTime: {Utilities.GetCurrentTimeInEST()}. " +
                                $"EndTimeEst: {endTimeEst}.", reservation.Id,
                                false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                            result.ReturnCode = HttpStatusCode.OK;

                            _logRepo.LogMessage($"Successfully changed latch response ReturnCode to OK, so it won't try to set it to expire again. " +
                               $"After reservation has expired in latch api when trying to edit latch reservation to expire. " +
                               $"ReservationId: {reservation.Id}. " +
                               $"Payload: {safeReservationRequest}. " +
                               $"Response: {result.ToJsonString()}. " +
                               $"CurrentEstTime: {Utilities.GetCurrentTimeInEST()}. " +
                               $"EndTimeEst: {endTimeEst}.", reservation.Id,
                               false, Utilities.GetCurrentTimeInEST(), LogType.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            _logRepo.LogMessage($"An error has occurred when latch response was RESERVATION_EXPIRED. " +
                                $"{editReason} " +
                                $"ReservationId: {reservation.Id}. " +
                                $"Payload: {safeReservationRequest}. " +
                                $"Response: {result.ToJsonString()}. " +
                                $"CurrentEstTime: {Utilities.GetCurrentTimeInEST()}. " +
                                $"EndTimeEst: {endTimeEst}. " +
                                $"Error: {ex.ToJsonString()}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                        }
                        catch
                        {

                        }
                    }
                }
                else
                {
                    _logRepo.LogMessage($"Failed to edit latch reservation (after reservation started). {editReason} ReservationId: {reservation.Id}. Payload: {safeReservationRequest}. Response: {result.ToJsonString()}. CurrentEstTime: {Utilities.GetCurrentTimeInEST()}. EndTimeEst: {endTimeEst}.", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        //private async Task<HttpStatusCode> EditBeforeReservationStarted(Reservation reservation, ReservationLatchData reservationLatchData, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime, List<string> keyIds)
        //{
        //    try
        //    {
        //        HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
        //        EditReservationRequest editReservationRequest = new EditReservationRequest(
        //                                      GetValidStartDateUTC(currentEstTime, reservation, isCronJob),
        //                                      DateTimeExtension.FromESTToUTCDateTime(_dateRepo.GetToDate(reservation, isCronJob, currentEstTime, true)),
        //                                      keyIds);

        //        string safeReservationRequest = ToSafeRequestJson(editReservationRequest);

        //        DateTime startTimeEst = editReservationRequest.StartTime.DateTime.ToESTDateTime();
        //        DateTime endTimeEst = editReservationRequest.EndTime.DateTime.ToESTDateTime();

        //        _logRepo.LogMessage($"Editing latch reservation (before reservation started). Reservation: {reservation.ToJsonString()}. Payload: {safeReservationRequest}. CurrentEstTime: {currentEstTime}. StartTimeEst: {startTimeEst}. EndTimeEst: {endTimeEst}.", reservation.Id, false, currentEstTime, LogType.Information);

        //        LatchReturn<EditCancelReservationResponse> result = await _latchReservationRepo.EditReservationAsync(editReservationRequest, reservation.Id, isCronJob, currentEstTime);
        //        statusCodeToReturn = result.ReturnCode;

        //        bool success = await SetReservationCodeStatusGetIsSuccess(reservation, reservationCode, CodeStatusAction.Edit, isCronJob, currentEstTime, result);
        //        if (success)
        //        {
        //            _logRepo.LogMessage($"Successfully Edited Latch Reservation (before reservation started). Reservation: {reservation.ToJsonString()}. Payload: {safeReservationRequest}. Response: {result.ToJsonString()}. CurrentEstTime: {currentEstTime}. StartTimeEst: {startTimeEst}. EndTimeEst: {endTimeEst}.", reservation.Id, false, currentEstTime, LogType.Information);
        //            await SaveLatchReservationStartDate(reservation, reservationLatchData, editReservationRequest.StartTime, isCronJob, currentEstTime);
        //            await SaveLatchReservationEndDate(reservation, reservationLatchData, editReservationRequest.EndTime, isCronJob, currentEstTime);
        //        }
        //        else
        //        {
        //            _logRepo.LogMessage($"Edit latch reservation failed  (before reservation started). Reservation: {reservation.ToJsonString()}. Payload: {safeReservationRequest}. CurrentEstTime: {currentEstTime}. StartTimeEst: {startTimeEst}. EndTimeEst: {endTimeEst}. Response: {result.ToJsonString()}", reservation.Id, false, currentEstTime, LogType.Error);
        //        }
        //        return statusCodeToReturn;
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        private async Task<HttpStatusCode> CancelLatchReservation(Reservation reservation, ReservationLatchData reservationLatchData, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime, string oldUnitId)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                _logRepo.LogMessage($"Canceling latch reservation. Reservation: {reservation.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                var buildingUnitId = _stayAccessDbContext.BuildingUnit.FirstOrDefault(x => x.UnitId == oldUnitId)?.Id ?? reservation.BuildingUnitId;
                var keyIds = await GetKeyIds(buildingUnitId);
                foreach (var keyToDelete in keyIds)
                {
                    var cancelReservationRequest = new CancelReservationRequest(reservationLatchData.UserUUid, keyToDelete);
                    LatchReturn<EditCancelReservationResponse> result = await _latchReservationRepo.CancelReservationAsync(cancelReservationRequest, reservation.Id, isCronJob, currentEstTime);

                    var isUnitDoor = _stayAccessDbContext.LockKey.Any(x => x.UUid == keyToDelete && x.BuildingUnitId != null);
                    //shouldn't retry if was failed because 'was already canceled' or not a unit door
                    statusCodeToReturn = result.ReturnMessage == "error: RESERVATION_CANCELLED - message: Reservation has already been cancelled" || ! isUnitDoor ? HttpStatusCode.OK : result.ReturnCode;
                    
                    if (isUnitDoor) //only mark success if unit door deleted successfully
                    {
                        bool success = await SetReservationCodeStatusGetIsSuccess(reservation, reservationCode, CodeStatusAction.Delete, isCronJob, currentEstTime, result);
                        if (success)
                        {
                            _logRepo.LogMessage($"Canceled latch reservation successfully. ReservationId: {reservation.Id}. Response: {result.ToJsonString()}. ", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                        }
                        else
                        {
                            if (result.ReturnMessage.Contains("RESERVATION_ALREADY_STARTED"))
                            {
                                _logRepo.LogMessage($"Canceling latch reservation didn't work in latch API. Attempting again, to 'Remove' this latch reservation. ReservationId: {reservation.Id}. Response: {result.ToJsonString()}. ", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                                statusCodeToReturn = await RemoveLatchReservationAsync(reservation, reservationLatchData, isCronJob, reservationCode, currentEstTime);
                            }
                            _logRepo.LogMessage($"Canceling latch reservation failed. Reservation: {reservation.ToJsonString()}. Response: {result.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                        }
                    }
                }
                return statusCodeToReturn;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<HttpStatusCode> UpdateLatchReservation(Reservation reservation, ReservationLatchData reservationLatchData, bool isCronJob, ReservationCode reservationCode, DateTime currentEstTime, string oldUnitId)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                _logRepo.LogMessage($"Updating latch reservation. Reservation: {reservation.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                var buildingUnitId = _stayAccessDbContext.BuildingUnit.FirstOrDefault(x => x.UnitId == oldUnitId)?.Id ?? reservation.BuildingUnitId;
                var keyIds = await GetKeyIds(buildingUnitId);
                var endDate = DateTimeExtension.FromESTToUTCDateTime(_dateRepo.GetToDate(reservation, isCronJob, currentEstTime, true));
                LatchReturn<EditCancelReservationResponse> result = null;
                foreach (var keyId in keyIds)
                {
                    var updateReservationRequest = new UpdateReservationRequest(reservationLatchData.UserUUid, keyId, endDate.ToString("yyyy-MM-ddTHH:mmZ"));
                    result = await _latchReservationRepo.EditReservationAsync(updateReservationRequest, reservation.Id, isCronJob, currentEstTime);
                    statusCodeToReturn = result.ReturnCode;
                    if (statusCodeToReturn != HttpStatusCode.OK) // if one door fails should retry the update
                    {
                        _logRepo.LogMessage($"Updating latch reservation failed. Reservation: {reservation.ToJsonString()}. Response: {result.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                        break;
                    }
                }                

                bool success = await SetReservationCodeStatusGetIsSuccess(reservation, reservationCode, CodeStatusAction.Edit, isCronJob, currentEstTime, result);
                if (success)
                {
                    await SaveLatchReservationEndDate(reservation, reservationLatchData, endDate, isCronJob, currentEstTime);
                    _logRepo.LogMessage($"Updated latch reservation successfully. ReservationId: {reservation.Id}. Response: {result.ToJsonString()}. ", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                }
                else
                {
                    _logRepo.LogMessage($"Updating latch reservation failed. Reservation: {reservation.ToJsonString()}. Response: {result.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                }
                return statusCodeToReturn;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string GetKeyToDelete(int buildingUnitId)
        {
            var lockKey = _stayAccessDbContext.LockKey.FirstOrDefault(lc => lc.BuildingUnitId == buildingUnitId);
            if (lockKey != null)
            {
                return lockKey.UUid;
            }
            return default;
        }

        private DateTime GetValidStartDateUTC(DateTime currentEstTime, Reservation reservation, bool isCronJob)
        {
            // method to prevent INVALID_START_TIME latch api error 
            DateTime dateRepoFromDateEst = _dateRepo.GetFromDate(reservation, BuildingLockSystem.Latch, reservation.Id, currentEstTime, isCronJob, true).ToESTDateTime();
            int minTooCloseToReservation = 1;
            DateTime startDateUTC = DateTime.Compare(currentEstTime.AddMinutes(minTooCloseToReservation), dateRepoFromDateEst) > 0
                                                    ? DateTimeExtension.FromESTToUTCDateTime(RoundUp(currentEstTime.AddMinutes(minTooCloseToReservation), TimeSpan.FromMinutes(10)))
                                                    : DateTimeExtension.FromESTToUTCDateTime(dateRepoFromDateEst);

            if (startDateUTC.ToESTDateTime() != reservation.FromDate().ToESTDateTime())
                _logRepo.LogMessage($"When going to latch api need to provide a valid startTime. " +
                                     $"Reservation.FromDate() was in the past or within {minTooCloseToReservation} minute(s) of currentTime. To prevent latch api from returning 'INVALID_START_TIME'" +
                                     $" Sending to latch api startTime(Est) : {startDateUTC.ToESTDateTime()}." +
                                     $" Reservation.FromDate() in Est: {reservation.FromDate().ToESTDateTime()}." +
                                     $" Configuration setting startTime(Est): {dateRepoFromDateEst}." +
                                     $" ReservationId: {reservation.Id}",
                                     reservation.Id, isCronJob, currentEstTime, LogType.Information);

            return startDateUTC;
        }

        private static bool IsSuccess<T>(LatchReturn<T> result) where T : ILatchResponse, new()
        {
            try
            {
                return APIResponseService.IsSuccessCode(result.ReturnCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<List<string>> GetKeyIds(int buildingUnitId)
        {
            try
            {
                List<string> keyIds = new();
                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    BuildingUnit buildingUnit = await dbContext.BuildingUnit.Where(x => x.Id == buildingUnitId).SingleOrDefaultAsync();
                    var lockKeysKeyIds = dbContext.LockKey.Where(x => (x.BuildingUnitId == buildingUnitId && x.BuildingUnitId != null)
                                                                  || (x.BuildingId == buildingUnit.BuildingId && x.BuildingId != null && x.UUid != null))
                                                    .Select(x => x.UUid)
                                                    .ToList();
                    if (lockKeysKeyIds != null)
                        keyIds = lockKeysKeyIds;
                }
                return keyIds;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<string> GetUnitId(int reservationId)
        {
            try
            {

                //get unit by reservationId because the reservation.buildingunitid can change when running a different code transaction before

                string unitId = string.Empty;
                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    var reservation = await dbContext.Reservation.Where(x => x.Id == reservationId).FirstOrDefaultAsync();
                    var buildingUnit = dbContext.BuildingUnit.FirstOrDefault(x => x.Id == reservation.BuildingUnitId);
                    if (buildingUnit != null)
                        unitId = buildingUnit.UnitId;
                }
                return unitId;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task SaveLatchReservationAndSetCodeStatusToActive(Reservation reservation, List<string> keyIds, CreateReservationResponse latchReturn, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                _logRepo.LogMessage($"Saving latchReservationToken in database and updating reservation's reservationCodes status to active. UserUUid: {latchReturn.UserUUid}. ReservationId: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);

                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                await using var dbContext = new StayAccessDbContext(optionsBuilder.Options);

                List<ReservationCode> reservationCodesToUpdate = await dbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id).ToListAsync();
                reservationCodesToUpdate.ForEach(x => { x.Status = CodeStatus.Active; x.ModifiedDate = DateTime.UtcNow; dbContext.Entry(x).State = EntityState.Modified; });

                var dbReservationLatchData = await dbContext.ReservationLatchData.Where(x => x.ReservationId == reservation.Id).FirstOrDefaultAsync();//SHOULD BE NULL!??

                if (dbReservationLatchData == default)
                {
                    _logRepo.LogMessage($"Adding ReservationLatchData record when attempting to save latchReservationToken in database," +
                        $" as no ReservationLatchData record found for this reservation." +
                        $" UserUUid: {latchReturn.UserUUid}. ReservationId: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);

                    var lockKey = _stayAccessDbContext.LockKey.FirstOrDefault(l => keyIds.Contains(l.UUid) && l.BuildingUnitId != null);
                    var returnUnit = latchReturn.Accesses.FirstOrDefault(x => x.DoorUUid == lockKey.UUid);
                    var unitCode = returnUnit.DoorCode.Code;
                    var buildingCode = latchReturn.Accesses.FirstOrDefault(x => x != returnUnit).DoorCode.Code;
                    ReservationLatchData reservationLatchData = new()
                    {
                        ReservationId = reservation.Id,
                        UserUUid = latchReturn.UserUUid,
                        CreatedDate = DateTime.UtcNow,
                        StartDateLatch = reservation.StartDate,
                        EndDateLatch = reservation.EndDate,
                        UnitCode = unitCode,
                        BuildingCode = buildingCode,
                        //IsActive = true
                    };

                    dbContext.Add(reservationLatchData);
                }
                else//when does it go into the else?
                {
                    _logRepo.LogMessage($"Found ReservationLatchData of id {dbReservationLatchData.Id} when attempting to save latchReservationToken in database." +
                        $" UserUUid: {latchReturn.UserUUid}. ReservationId: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    dbReservationLatchData.UserUUid = latchReturn.UserUUid;
                    dbReservationLatchData.ModifiedDate = DateTime.UtcNow;
                    dbContext.Entry(dbReservationLatchData).State = EntityState.Modified;
                }
                dbContext.SaveChanges();
                _logRepo.LogMessage($"Successfully saved in database Reservation.LatchReservationToken and updated reservation's reservationCodes status to active. UserUUid: {latchReturn.UserUUid}. ReservationId: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Failed to save LatchReservationToken in database and updating reservation's reservationCodes status to active. UserUUid: {latchReturn.UserUUid}. ReservationId: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }

        //private async Task UpdateAndSaveNewReservationChanges(int reservationId, string reservationToken, DateTime originalStartDate, DateTime startDateUTC, bool isCronJob, DateTime currentEstTime)
        //{
        //    try
        //    {
        //        bool startDateChanged = DateTime.Compare(originalStartDate, startDateUTC) != 0;
        //        _logRepo.LogMessage($"Saving latchReservationToken. " +
        //            $"{(startDateChanged ? $"Updating startDate to exactly what was sent to latch Api. Original startTime: {originalStartDate}. Changed To: {startDateUTC}" : "")}" +
        //            $" ReservationId: {reservationId}", isCronJob, currentEstTime);

        //        var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
        //        optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
        //        await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
        //        {
        //            var dbReservation = dbContext.Reservation.FirstOrDefault(x => x.Id == reservationId);
        //            dbReservation.LatchReservationToken = reservationToken;
        //            dbReservation.StartDate = startDateUTC;
        //            dbContext.Entry(dbReservation).State = EntityState.Modified;
        //            dbContext.SaveChanges();
        //            _logRepo.LogMessage($"Successfully saved changes in the database for latch reservation. " +
        //                $"Reservation.LatchReservationToken was saved. " +
        //                $"{(startDateChanged ? $" The Reservation.StartDate was updated to the startTime that this reservation was set to in latch api. Original startTime: {originalStartDate}. Changed To: {startDateUTC}" : "")}" +
        //                $" ReservationId: {dbReservation.Id}", isCronJob, currentEstTime);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logRepo.LogMessage($"Failed to save changes in database for latch reservation. " +
        //            $"The LatchReservationToken failed to save. " +
        //               $"{(DateTime.Compare(originalStartDate, startDateUTC) != 0 ? $" The Reservation.startDate wasn't updated to the startTime that this reservation was set to in latch api. Original startTime: {originalStartDate}. Changed To: {startDateUTC}" : "")}" +
        //               $". ReservationId: {reservationId}",
        //           isCronJob, currentEstTime);
        //        throw;
        //    }
        //}

        private async Task SaveLatchReservationStartDate(Reservation reservation, ReservationLatchData reservationLatchData, DateTimeOffset latchStartTimeExactlyHowSentToLatch, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                GetReservationInfo(reservation, out int reservationId, out DateTime originalDatabaseDataEitherEarlyCheckInOrStartDate);

                DateTime startDateInLatchEstFormat = GetStartDateInLatchEstFormat(latchStartTimeExactlyHowSentToLatch);

                _logRepo.LogMessage($"Saving ReservationLatchData.LatchStartDate." +
                    $" Reservation.startDate/EarlyCheckIn: {originalDatabaseDataEitherEarlyCheckInOrStartDate}." +
                    $" LatchStartTime in latch api(Est): {startDateInLatchEstFormat}" +
                    $" ReservationId: {reservationId}", reservationId, isCronJob, currentEstTime, LogType.Information);

                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    var dbReservationLatchData = dbContext.ReservationLatchData.FirstOrDefault(x => x.ReservationId == reservationId);
                    dbReservationLatchData.StartDateLatch = ((DateTime)latchStartTimeExactlyHowSentToLatch.UtcDateTime).ToESTDateTime();
                    dbReservationLatchData.ModifiedDate = DateTime.UtcNow;
                    dbContext.Entry(dbReservationLatchData).State = EntityState.Modified;
                    dbContext.SaveChanges();

                    _logRepo.LogMessage($"Successfully saved ReservationLatchData.LatchStartDate." +
                        $" Reservation.startDate/EarlyCheckIn: {originalDatabaseDataEitherEarlyCheckInOrStartDate}." +
                        $" LatchStartTime in latch api(Est): {startDateInLatchEstFormat}" +
                        $" ReservationId: {reservationId}", reservationId, isCronJob, currentEstTime, LogType.Information);
                }
            }
            catch (Exception ex)
            {
                GetReservationInfo(reservation, out int reservationId, out DateTime originalDatabaseDataEitherEarlyCheckInOrStartDate);

                _logRepo.LogMessage($"Failed to save ReservationLatchData.LatchStartDate." +
                    $" Reservation.startDate/EarlyCheckIn: {originalDatabaseDataEitherEarlyCheckInOrStartDate}." +
                    $" LatchStartTime in latch api(Est): {GetStartDateInLatchEstFormat(latchStartTimeExactlyHowSentToLatch)}" +
                    $" ReservationId: {reservationId}", reservationId, isCronJob, currentEstTime, LogType.Error);
                throw;
            }

            static DateTime GetStartDateInLatchEstFormat(DateTimeOffset latchStartTimeExactlyHowSentToLatch)
            {
                return latchStartTimeExactlyHowSentToLatch.UtcDateTime.ToESTDateTime();
            }

            static void GetReservationInfo(Reservation reservation, out int reservationId, out DateTime originalDatabaseDataEitherEarlyCheckInOrStartDate)
            {
                reservationId = reservation.Id;
                originalDatabaseDataEitherEarlyCheckInOrStartDate = reservation.FromDate();
            }
        }

        private async Task SaveLatchReservationEndDate(Reservation reservation, ReservationLatchData reservationLatchData, DateTimeOffset latchEndTimeExactlyHowSentToLatch, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                GetReservationInfo(reservation, out int reservationId, out DateTime originalDatabaseDataEitherLateCheckoutOrEndDate);

                DateTime startDateInLatchEstFormat = GetEndDateInLatchEstFormat(latchEndTimeExactlyHowSentToLatch);

                _logRepo.LogMessage($"Saving ReservationLatchData.LatchEndDate." +
                    $" Reservation.EndDate/LateCheckOut: {originalDatabaseDataEitherLateCheckoutOrEndDate}." +
                    $" LatchStartTime in latch api(Est): {startDateInLatchEstFormat}" +
                    $" ReservationId: {reservationId}", reservationId, isCronJob, currentEstTime, LogType.Information);

                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    var dbReservationLatchData = dbContext.ReservationLatchData.FirstOrDefault(x => x.ReservationId == reservationId);
                    dbReservationLatchData.EndDateLatch = ((DateTime)latchEndTimeExactlyHowSentToLatch.UtcDateTime).ToESTDateTime();
                    dbReservationLatchData.ModifiedDate = DateTime.UtcNow;
                    dbContext.Entry(dbReservationLatchData).State = EntityState.Modified;
                    dbContext.SaveChanges();

                    _logRepo.LogMessage($"Successfully saved ReservationLatchData.LatchEndDate." +
                        $" Reservation.EndDate/LateCheckOut {originalDatabaseDataEitherLateCheckoutOrEndDate}." +
                        $" LatchStartTime in latch api(Est): {startDateInLatchEstFormat}" +
                        $" ReservationId: {reservationId}", reservationId, isCronJob, currentEstTime, LogType.Information);
                }
            }
            catch (Exception ex)
            {
                GetReservationInfo(reservation, out int reservationId, out DateTime originalDatabaseDataEitherLateCheckoutOrEndDate);

                _logRepo.LogMessage($"Failed to save ReservationLatchData.LatchEndDate." +
                    $" Reservation.EndDate/LateCheckOut: {originalDatabaseDataEitherLateCheckoutOrEndDate}." +
                    $" LatchStartTime in latch api(Est): {GetEndDateInLatchEstFormat(latchEndTimeExactlyHowSentToLatch)}" +
                    $" ReservationId: {reservationId}", reservationId, isCronJob, currentEstTime, LogType.Error);
                throw;
            }

            static DateTime GetEndDateInLatchEstFormat(DateTimeOffset requestEndTime)
            {
                return requestEndTime.UtcDateTime.ToESTDateTime();
            }

            static void GetReservationInfo(Reservation reservation, out int reservationId, out DateTime originalDatabaseDataEitherLateCheckoutOrEndDate)
            {
                reservationId = reservation.Id;
                originalDatabaseDataEitherLateCheckoutOrEndDate = reservation.ToDate();
            }
        }

        private async Task<(string permanentUnitCode, string buildingCode, bool admittanceExpired)> FetchAndSaveLatchReservationCodes(Reservation reservation, string latchReservationToken, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                _logRepo.LogMessage($"Fetching door codes from latch API and saving in ReservationLatchData.", reservation.Id, isCronJob, currentEstTime, LogType.Information);

                LatchReturn<GetDoorCodesResponse> latchReturn = await _latchIntegrationService.GetReservationDoorCodesAsync(new() { LatchReservationToken = latchReservationToken }, reservation.Id, isCronJob, currentEstTime);

                if (APIResponseService.IsSuccessCode(latchReturn.ReturnCode))
                {
                    _logRepo.LogMessage($"Fetch door codes latch API return code is successful.", reservation.Id, isCronJob, currentEstTime, LogType.Information);

                    if (latchReturn.Response.payload.message == default)
                    {
                        throw new Exception($"FetchAndSaveLatchReservationCodes method. latchReturn.Response.payload.message is default. ReservationId: {reservation.Id}. LatchReservationToken: {latchReservationToken}.");
                    }

                    //get unit id now. Don't send in as input parameter, because the unitId can change from a recent reservation 'update' to new unit.
                    string unitId = await GetUnitId(reservation.Id);

                    var permanentCodes = latchReturn.Response.payload.message.Where(x => x.PasscodeType == "PERMANENT");

                    string buildingCode = permanentCodes.Where(x => x.LockName != unitId).Select(x => x.DoorCode).Distinct().SingleOrDefault();
                    string permanentUnitCode = permanentCodes.Where(x => x.LockName == unitId).Select(x => x.DoorCode).Distinct().SingleOrDefault();


                    if (buildingCode == default)
                    {
                        _logRepo.LogMessage($"After fetch door code Latch API: var buildingCode is default. unitId: {unitId}. latchReturn.Response.payload.message: {latchReturn.Response.payload.message.ToJsonString()}.", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    }

                    if (permanentUnitCode == default)
                    {
                        _logRepo.LogMessage($"After fetch door code Latch API: var unitCode is default. unitId: {unitId}. latchReturn.Response.payload.message: {latchReturn.Response.payload.message.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    }

                    var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                    optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                    await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                    {
                        var dbReservationLatchData = dbContext.ReservationLatchData.FirstOrDefault(x => x.ReservationId == reservation.Id);
                        dbReservationLatchData.UnitCode = permanentUnitCode;
                        dbReservationLatchData.BuildingCode = buildingCode;
                        dbReservationLatchData.ModifiedDate = DateTime.UtcNow;
                        dbContext.Entry(dbReservationLatchData).State = EntityState.Modified;
                        dbContext.SaveChanges();

                    }

                    _logRepo.LogMessage($"Successfully hit fetched door codes latch API and saved in ReservationLatchData (may have set buildingCode or unitCode to default if there wasn't any found in latchReturn.Response.payload.message).", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    return (permanentUnitCode, buildingCode, false);
                }
                else
                {
                    if (latchReturn?.ReturnMessage != null && latchReturn.ReturnMessage.Contains($"ADMITTANCE_EXPIRED"))
                    {
                        _logRepo.LogMessage($"Successfully catching ADMITTANCE_EXPIRED return message, when unsuccessful latch ReturnCode. ",
                            reservation.Id, isCronJob, currentEstTime, LogType.Information);
                        return (default, default, true);
                    }
                    else
                    {
                        throw new Exception($"Unsuccessful status code returned from Latch API, when attempting to fetch latch reservation codes. " +
                            $"latchReturn (deserialized and converted to object): {latchReturn.ToJsonString()}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Error occurred in 'fetching door codes from latch API and saving in ReservationLatchData' method. Error: {ex.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }

        private enum CodeStatusAction
        {
            Create = 1,
            Delete = 2,
            Edit = 3,
            EditToExpire = 4,
        }

        private static string ToSafeRequestJson<T>(T request) where T : LatchRequest
        {
            try
            {
                string serialized = JsonConvert.SerializeObject(request);
                T clonedObject = JsonConvert.DeserializeObject<T>(serialized);
                clonedObject.Token = "hidden";
                //  clonedObject = "hidden";
                // clonedObject.SecretKey = "hidden";
                return clonedObject.ToJsonString();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task CaptureLatchCodesSaveAndThenSendToMRI(Reservation reservation, List<string> keyIds, CreateReservationResponse latchReturn, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                var lockKey = _stayAccessDbContext.LockKey.FirstOrDefault(l => keyIds.Contains(l.UUid) && l.BuildingUnitId != null);
                var returnUnit = latchReturn.Accesses.FirstOrDefault(x => x.DoorUUid == lockKey.UUid);
                var unitCode = returnUnit.DoorCode.Code;
                var buildingCode = latchReturn.Accesses.FirstOrDefault(x => x != returnUnit).DoorCode.Code;
                string payload = $@"
                     {{
                         ""ReservationId"": {reservation.Id},
                         ""LatchUrl"":  ""{null}"",
                         ""UnitCode"": ""{unitCode}"",
                         ""BuildingCode"": ""{buildingCode}"",
                         
                     }}";

                await _apiProviderRepo.PostStringToMri(reservation.Id, "StayAccess/SaveLatchLink", payload, currentEstTime, isCronJob);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> SetReservationCodeStatusGetIsSuccess<T>(Reservation reservation, ReservationCode reservationCode, CodeStatusAction codeStatusAction, bool isCronJob, DateTime currentEstTime, LatchReturn<T> response) where T : ILatchResponse, new()
        {
            try
            {
                bool success = IsSuccess(response);
                //catch unsuccessful response codes, in situations where it shouldn't error because of it.
                if (!success
                    //&& (codeStatusAction == CodeStatusAction.Delete || codeStatusAction == CodeStatusAction.EditToExpire) && 
                    &&
                    (response?.ReturnMessage?.Contains("RESERVATION_EXPIRED") == true || response?.ReturnMessage?.Contains("RESERVATION_CANCELLED") == true))
                {
                    //if it already was deleted no need to try deleting it again.
                    success = true;
                    _logRepo.LogMessage($"SetReservationCodeStatusGetIsSuccess changing success from 'false' to 'true', to catch the latch API error that isn't really an error in this case." +
                        $" Response: {response.ToJsonString()}.", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                }
                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                await using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    string logMessage = isCronJob ? "Automate - " : string.Empty;

                    // update reservation code status in database
                    var reservationCodeToUpdate = dbContext.ReservationCode.FirstOrDefault(x => x.Id == reservationCode.Id);
                    CodeStatus reservationCodeStatus = reservationCodeToUpdate.Status;

                    if (reservationCodeToUpdate != null)
                    {
                        if (success)
                            switch (codeStatusAction)
                            {
                                case CodeStatusAction.Delete:
                                    reservationCodeToUpdate.Status = CodeStatus.Deleted;
                                    break;
                                case CodeStatusAction.Create:
                                    reservationCodeToUpdate.Status = CodeStatus.Active;
                                    break;
                                case CodeStatusAction.Edit:
                                    reservationCodeToUpdate.Status = CodeStatus.Active;
                                    break;
                                case CodeStatusAction.EditToExpire:
                                    reservationCodeToUpdate.Status = CodeStatus.Deleted;
                                    break;
                            }
                        else
                            switch (codeStatusAction)
                            {
                                case CodeStatusAction.Delete:
                                    reservationCodeToUpdate.Status = CodeStatus.DeleteFailed;
                                    break;
                                case CodeStatusAction.Create:
                                    reservationCodeToUpdate.Status = CodeStatus.Pending;
                                    break;
                                case CodeStatusAction.Edit:
                                    reservationCodeToUpdate.Status = CodeStatus.ActiveFailed;
                                    break;
                                case CodeStatusAction.EditToExpire:
                                    reservationCodeToUpdate.Status = CodeStatus.DeleteFailed;
                                    break;
                            }
                        // update reservation code
                        reservationCodeToUpdate.ModifiedDate = DateTime.UtcNow;
                        dbContext.Entry(reservationCodeToUpdate).State = EntityState.Modified;
                        logMessage += $"Reservation code status updated from {reservationCodeStatus} to {reservationCodeToUpdate.Status} for latch door at {currentEstTime} in EST. When attempted to {codeStatusAction} reservation. ReservationId: {reservation.Id}.";
                    }
                    else
                    {
                        logMessage += $"No reservation code found to update code status for latch door at {currentEstTime} in EST. When attempted to {codeStatusAction} reservation. ReservationId: {reservation.Id}.";
                    }
                    // add logger info
                    dbContext.Logger.Add(new Logger
                    {
                        CreatedDate = DateTime.UtcNow,
                        LogTypeId = LogType.Information,
                        Message = logMessage,
                        ReservationId = reservation.Id,
                    });

                    // save changes to database
                    dbContext.SaveChanges();

                    return success;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            //rounds number up to the nearest of the TimeStan input parameter
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }
    }
}
