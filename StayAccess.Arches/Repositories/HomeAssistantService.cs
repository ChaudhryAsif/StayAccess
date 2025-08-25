using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StayAccess.Arches.Interfaces;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Doors.ArchesFrontDoor;
using StayAccess.DTO.Email;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.HomeAssistant;
using StayAccess.Tools;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using StayAccess.Tools.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StayAccess.Arches.Repositories
{
    public class HomeAssistantService : IHomeAssistantService
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerService<HomeAssistantService> _loggerRepo;
        private readonly StayAccessDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly HomeAssistantEndPoints _homeAssistantEndPoints;
        private readonly ILogService _logRepo;
        private readonly IDateService _dateRepo;
        private static int _millisecond;

        public HomeAssistantService(IConfiguration configuration, ILoggerService<HomeAssistantService> loggerRepo, StayAccessDbContext dbContext, IOptions<HomeAssistantEndPoints> homeAssistantEndPoints, IEmailService emailService, ILogService logRepo, IDateService dateService)
        {
            _configuration = configuration;
            _loggerRepo = loggerRepo;
            _dbContext = dbContext;
            _emailService = emailService;
            _homeAssistantEndPoints = homeAssistantEndPoints.Value;
            _millisecond = 0;
            _logRepo = logRepo;
            _dateRepo = dateService;
        }

        #region Public Method -> Unit

        public void AdjustCommmandDtoDatesTmeOfDay(CommandDto commandDto, bool isCronJob = false)
        {
            try
            {
                _loggerRepo.Add(LogType.Information, $"Adjusting commandDto time of day of 'from and 'to' dates. Original 'from': {commandDto.from}. Original 'to': {commandDto.to}", null);
                commandDto.from = _dateRepo.GetFromDate(commandDto.from, BuildingLockSystem.Arches, null, Utilities.GetCurrentTimeInEST(), isCronJob, false);
                commandDto.to = _dateRepo.GetToDate(commandDto.to, null, isCronJob, Utilities.GetCurrentTimeInEST(), true);
                _loggerRepo.Add(LogType.Information, $"Adjusted commandDto time of day of 'from and 'to' dates. Current Dates. From: {commandDto.from}. To: {commandDto.to}", null);
            }
            catch (Exception)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred when adjusting commandDto time of day of 'from and 'to' dates.", null);
                throw;
            }
        }

        public HttpStatusCode CreateDeleteUnitCode(string action, string unit, Reservation reservation, ReservationCode reservationCode, int codeTransactionId, DateTime currentEstTime, bool isCronJob = false)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                CommandDto unitCommandDto = new()
                {
                    action = action,
                    from = _dateRepo.GetFromDate(reservation, BuildingLockSystem.Arches, reservation.Id, currentEstTime, isCronJob, false),
                    to = _dateRepo.GetToDate(reservation, isCronJob, currentEstTime, true),
                    unit = unit.ToLower(),
                    reservation = reservation.Code,
                    code = reservationCode.LockCode,
                    slot = reservationCode.SlotNo.ToString(),
                    codeTransactionId = codeTransactionId
                };

                // set unit codes using home assistant api
                bool unitSuccess = ExecuteCommandsForUnitAsync(unitCommandDto, reservation.Id).GetAwaiter().GetResult();

                if (!unitSuccess)
                {
                    // retry sending/deleting code
                    unitSuccess = ExecuteCommandsForUnitAsync(unitCommandDto, reservation.Id).GetAwaiter().GetResult();
                }

                if (unitSuccess)
                {
                    statusCodeToReturn = HttpStatusCode.OK;
                }

                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    string logMessage = isCronJob ? "Automate - " : string.Empty;

                    // update reservation code status in database
                    var reservationCodeToUpdate = dbContext.ReservationCode.FirstOrDefault(x => x.Id == reservationCode.Id);
                    if (reservationCodeToUpdate != null)
                    {
                        if (unitSuccess)
                            reservationCodeToUpdate.Status = action == "delete" ? CodeStatus.Deleted : CodeStatus.Active;
                        else
                            reservationCodeToUpdate.Status = action == "delete" ? CodeStatus.DeleteFailed : CodeStatus.ActiveFailed;

                        // update reservation code
                        reservationCodeToUpdate.ModifiedDate = DateTime.UtcNow;
                        dbContext.Entry(reservationCodeToUpdate).State = EntityState.Modified;
                        logMessage += $"Reservation code status updated at {currentEstTime} in EST for Command: {unitCommandDto.ToJsonString()}.";
                    }
                    else
                    {
                        logMessage += $"No reservation code found to update status at {currentEstTime} in EST for Command: {unitCommandDto.ToJsonString()}.";
                    }

                    // add logger info
                    dbContext.Logger.Add(new DAL.DomainEntities.Logger
                    {
                        CreatedDate = DateTime.UtcNow,
                        LogTypeId = LogType.Information,
                        Message = logMessage,
                        ReservationId = reservation.Id,
                    });

                    // save changes to database
                    dbContext.SaveChanges();
                }
                return statusCodeToReturn;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Set device lock codes on HomeAssistant api, if reservation is active
        /// </summary>
        /// <param name="reservation"></param>
        public void SetCodesForUnit(Reservation reservation, List<ReservationCode> reservationCodes, bool isCronJob = false, Reservation oldReservation = null)
        {
            try
            {
                if (reservation != null && reservation.IsCurrentActiveReservation())
                {
                    if (reservationCodes != null && reservationCodes.Any())
                    {
                        foreach (var reservationCode in reservationCodes)
                        {
                            if (reservationCode.Status != CodeStatus.Active)
                            {
                                SetReservationCode(reservation, reservationCode, "create", isCronJob);
                            }
                            else
                            {
                                // implement new process of "Modify Code" for "Front Door" and update pannel
                                if (oldReservation != null)
                                {
                                    ModifiedCodeForFrontDoorAsync(reservation).GetAwaiter().GetResult();

                                    if (oldReservation.BuildingUnitId != reservation.BuildingUnitId)
                                        AutoCreateUnitCodeForNewUnit(reservation, isCronJob);
                                }
                            }
                        }
                    }
                    else
                    {
                        _loggerRepo.Add(LogType.Information, $"No device codes for reservation: {reservation.Id}.", reservation.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error: {ex.Message}. Setting device codes failed for reservation: {reservation.Id}.", reservation.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete code
        /// </summary>
        /// <param name="reservationCode"></param>
        /// <returns></returns>
        public void DeleteCodesForUnit(Reservation reservation, List<ReservationCode> reservationCodes, bool isCronJob = false, bool deleteFrontDoorCode = false)
        {
            try
            {
                if (reservation != null && reservationCodes.HasAny())
                {
                    foreach (var reservationCode in reservationCodes)
                    {
                        if (reservationCode.Status == CodeStatus.Active || reservationCode.Status == CodeStatus.DeleteFailed)
                        {
                            SetReservationCode(reservation, reservationCode, "delete", isCronJob, deleteFrontDoorCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error: {ex.Message}. Deleting device codes failed.", reservation.Id);
                throw;
            }
        }

        /// <summary>
        /// Execute device commands to set/delete device codes
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<bool> ExecuteCommandsForUnitAsync(CommandDto commands, int? reservationId)
        {
            try
            {
                _loggerRepo.Add(LogType.Information, $"Executing commands for unit. CommandDto: {commands.ToJsonString()}", reservationId);

                // execute device commands to set/delete device codes
                HttpResponseMessage response = await PostRequestForUnitAsync(_homeAssistantEndPoints.ForUnit, commands, HomeAssistantClient.Unit);

                if (response.IsSuccessStatusCode)
                {
                    _loggerRepo.Add(LogType.Information, $"Successfully hit api on executed commands for unit. CommandDto: {commands.ToJsonString()}. ResponseStatusCode: {response.StatusCode}", reservationId);
                }

                //response code of response, after successfully hitting the api
                bool isSuccessResponseStatusCode = APIResponseService.IsSuccessCode(response.StatusCode);
                if (!isSuccessResponseStatusCode)
                {
                    _loggerRepo.Add(LogType.Error, $"Response status code wasn't successful, after hitting executed commands for unit API. ResponseStatusCode: {response.StatusCode} CommandDto: {commands.ToJsonString()}.", reservationId);
                }

                return isSuccessResponseStatusCode;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> DeleteArchesBuildingUnitCodeAsync(int unitSlotLogId)
        {
            try
            {
                var unitSlotLog = _dbContext.UnitSlotLog.FirstOrDefault(x => x.Id == unitSlotLogId);
                if (unitSlotLog is null)
                    throw new Exception("UnitSlotLogId is invalid");

                CommandDto unitCommandDto = new()
                {
                    action = "delete",
                    unit = unitSlotLog.Unit.ToLower(),
                    code = unitSlotLog.Code,
                    slot = unitSlotLog.Slot
                };

                // delete device code using home assistant api
                bool unitSuccess = await ExecuteCommandsForUnitAsync(unitCommandDto, null);

                if (!unitSuccess)
                {
                    // retry deleting code
                    unitSuccess = await ExecuteCommandsForUnitAsync(unitCommandDto, null);
                }

                return unitSuccess;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Public Method -> Front Door

        /// <summary>
        /// create codes for front door
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="reservationCode"></param>
        /// <returns></returns>
        public async Task<HttpStatusCode> CreateCodeForFrontDoorAsync(Reservation reservation, ReservationCode reservationCode, int codeTransactionId, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                string unitId = GetUnitId(reservation.BuildingUnitId);

                CreateRequestFrontDoorDto createFrontDoorDto = new()
                {
                    FirstName = $"GLS {unitId}",
                    LastName = $"{reservation.Code}",
                    StartedOn = DateTimeExtension.FromESTToUTCDateTime(_dateRepo.GetFromDate(reservation, BuildingLockSystem.Arches, reservation.Id, currentEstTime, isCronJob, false)),
                    ExpiresOn = DateTimeExtension.FromESTToUTCDateTime(_dateRepo.GetToDate(reservation, isCronJob, currentEstTime, true)),
                    SecurityLevelId = 1,
                    IsMaster = false,
                    IsSupervisor = false,
                    CanTripleSwipe = false,
                    CanDisengageEmergencyAlarm = false,
                    HandicapOpener = false,
                    CardholderImage = null,
                    AccessoryImage1 = null,
                    AccessoryImage2 = null,
                    UserSource = 0,
                    AlternateId1 = null,
                    AlternateId2 = null,
                    CustomFields = new List<CustomFields>() { new CustomFields() { Name = "Apartment Number", Value = unitId } },
                    Cards = new List<Cards>() { new Cards() { SiteCode = 0, CardNumber = 0, PinNumber = reservationCode.LockCode } },
                    AccessGroups = new List<int>() { 285 },
                    Partitions = new List<int>() { 5 },
                    CodeTransactionId = codeTransactionId
                };

                //_logRepo.LogMessage($"Creating code for front door with payload: {createFrontDoorDto.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                CreateResponseFrontDoorDto response = await RequestAsync<CreateRequestFrontDoorDto, CreateResponseFrontDoorDto>(HttpMethod.Post, _homeAssistantEndPoints.CreateFrontDoor, HomeAssistantClient.FrontDoor, createFrontDoorDto);
                if (response != null)
                {
                    if (response.Id > 0)
                    {
                        // save "FrontDoorUserId" in reservation table in database
                        SaveFrontDoorUserId(reservation.Id, response.Id);
                        _logRepo.LogMessage($"Code created successfully for front door with payload: {createFrontDoorDto.ToJsonString()}. Response: {response.ToJsonString()}.", reservation.Id, isCronJob, currentEstTime, LogType.Information);

                        _logRepo.LogMessage($"Updating panel info after creating front door code with payload: {createFrontDoorDto.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                        var test123 = await UpdatePannelFrontDoorAsync();
                        _logRepo.LogMessage($"test123 c: {test123.ToJsonString()}", reservation.Id, isCronJob, Utilities.GetCurrentTimeInEST(), LogType.Information);
                        _logRepo.LogMessage($"Panel info updated after creating front door code with payload: {createFrontDoorDto.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                        statusCodeToReturn = HttpStatusCode.OK;
                    }
                    else
                    {
                        _logRepo.LogMessage($"Code creation failed for front door with payload: {createFrontDoorDto.ToJsonString()}. Response: {response.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                    }
                }
                else
                {
                    _logRepo.LogMessage($"Code creation failed for front door with payload: {createFrontDoorDto.ToJsonString()}. Error: response is null", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                }
                return statusCodeToReturn;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// get existing user front door
        /// </summary>
        /// <param name="reservation"></param>
        /// <returns></returns>
        public async Task<ExistingUserResponseFrontDoorDto> GetExistingUserFrontDoorAsync(string buildingUnitId, int reservationId, string reservationCode)
        {
            try
            {
                string existingUserUrl = _homeAssistantEndPoints.GetExistingUserUrlForFrontDoor(buildingUnitId, reservationCode);
                _logRepo.LogMessage($"existing User Url : {existingUserUrl}", reservationId, true, Utilities.GetCurrentTimeInEST(), LogType.Information);
                var response = await RequestAsync<object, ExistingUserResponseFrontDoorDto>(HttpMethod.Get, existingUserUrl, HomeAssistantClient.FrontDoor);
                _logRepo.LogMessage($"existing User Url : {existingUserUrl}. Response : {response}", reservationId, true, Utilities.GetCurrentTimeInEST(), LogType.Information);
                return response;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Error while getting existing user for front door unit : {buildingUnitId}, reservation code : {reservationCode}. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", reservationId, true, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// delete code for front door
        /// </summary>
        /// <param name="reservation"></param>
        /// <returns></returns>
        public async Task<HttpStatusCode> DeleteCodeForFrontDoorAsync(Reservation reservation, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                if (reservation.FrontDoorUserId.HasValue)
                {
                    string url = _homeAssistantEndPoints.GetModifiedOrDeleteCodeUrlForFrontDoor(reservation.FrontDoorUserId.Value);

                    _logRepo.LogMessage($"Deleting front door code for reservation: {reservation.ToJsonString()} with url {url}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    var test123 = await RequestAsync<object, object>(HttpMethod.Delete, url, HomeAssistantClient.FrontDoor);
                    _logRepo.LogMessage($"test123 b: {test123.ToJsonString()}", reservation.Id, isCronJob, Utilities.GetCurrentTimeInEST(), LogType.Information);
                    _logRepo.LogMessage($"Front door code deleted successfully for reservation: {reservation.ToJsonString()} with url {url}", reservation.Id, isCronJob, currentEstTime, LogType.Information);

                    _logRepo.LogMessage($"Updating panel info after deleting front door code for reservation: {reservation.ToJsonString()} with url {url}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    var updatePanelResponse = await UpdatePannelFrontDoorAsync(); //var test123 =
                    _logRepo.LogMessage($"test123 e: {updatePanelResponse.ToJsonString()}", reservation.Id, isCronJob, Utilities.GetCurrentTimeInEST(), LogType.Information);
                    _logRepo.LogMessage($"Panel info updated successfully after deleting front door code for reservation: {reservation.ToJsonString()} with url : {url}. update panel response : {updatePanelResponse.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Information);
                    statusCodeToReturn = HttpStatusCode.OK;
                }
                else
                {
                    _logRepo.LogMessage($"UserId not found in reservation Id: {reservation.Id}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                }
                return statusCodeToReturn;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Error occurred in DeleteCodeForFrontDoorAsync. Exception: {ex.ToJsonString()}", reservation.Id, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// delete or modified code for front door
        /// </summary>
        /// <param name="oldReservation"></param>
        /// <param name="reservation"></param>
        /// <returns></returns>
        public async Task<HttpStatusCode> ModifiedCodeForFrontDoorAsync(Reservation reservation, bool isCronJob = false)
        {
            try
            {
                HttpStatusCode statusCodeToReturn = HttpStatusCode.BadRequest;
                DateTime currentTimeInEst = Utilities.GetCurrentTimeInEST();
                if (reservation.FrontDoorUserId.HasValue)
                {
                    string url = _homeAssistantEndPoints.GetModifiedOrDeleteCodeUrlForFrontDoor(reservation.FrontDoorUserId.Value);
                    string newUnit = GetUnitId(reservation.BuildingUnitId);

                    ModifyCodeRequestFrontDoorDto modifyCodeFrontDoor = new()
                    {
                        Properties = new List<Properties>()
                            {
                                new Properties(){ Name = "FirstName", Value = $"GLS {newUnit}" },
                                new Properties(){ Name = "LastName", Value = reservation.Code },
                                new Properties(){ Name = "Custom_Apartment Number", Value = newUnit },
                                new Properties(){ Name = "ExpiresOn", Value = DateTimeExtension.FromESTToUTCDateTime(_dateRepo.GetToDate(reservation, isCronJob, currentTimeInEst, true)) }
                            }
                    };

                    _logRepo.LogMessage($"Modifying code of front door for reservation: {reservation.ToJsonString()} with payload {modifyCodeFrontDoor.ToJsonString()}", reservation.Id, false, currentTimeInEst, LogType.Information);
                    var test123 = await RequestAsync<ModifyCodeRequestFrontDoorDto, ModifyResponseFrontDoorDto>(HttpMethod.Put, url, HomeAssistantClient.FrontDoor, modifyCodeFrontDoor);
                    _logRepo.LogMessage($"test123 a: {test123.ToJsonString()}", reservation.Id, isCronJob, Utilities.GetCurrentTimeInEST(), LogType.Information);
                    _logRepo.LogMessage($"Code modified successfully of front door for reservation: {reservation.ToJsonString()} with payload {modifyCodeFrontDoor.ToJsonString()}", reservation.Id, false, currentTimeInEst, LogType.Information);


                    try
                    {
                        _logRepo.LogMessage($"Updating panel info after modifying front door code: {reservation.ToJsonString()} with payload {modifyCodeFrontDoor.ToJsonString()}", reservation.Id, false, currentTimeInEst, LogType.Information);
                        var test1234 = await UpdatePannelFrontDoorAsync();
                        _logRepo.LogMessage($"test123 d: {test1234.ToJsonString()}", reservation.Id, isCronJob, Utilities.GetCurrentTimeInEST(), LogType.Information);
                        _logRepo.LogMessage($"Panel info updated after modifying front door code: {reservation.ToJsonString()} with payload {modifyCodeFrontDoor.ToJsonString()}", reservation.Id, false, currentTimeInEst, LogType.Information);
                        statusCodeToReturn = HttpStatusCode.OK;
                    }
                    catch (Exception ex)
                    {
                        _logRepo.LogMessage($"Failed to update panel info after modifying front door code: {reservation.Id}. Error: {ex}", reservation.Id, false, currentTimeInEst, LogType.Error);
                    }
                }
                else
                {
                    _logRepo.LogMessage($"UserId not found in reservation Id: {reservation.Id}.", reservation.Id, false, currentTimeInEst, LogType.Error);
                }
                return statusCodeToReturn;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"An error occurred in modifying code of front door for reservation: {reservation.Id}. Error: {ex}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                return HttpStatusCode.BadRequest;
            }
        }

        /// <summary>
        /// update pannel front door
        /// </summary>
        /// <returns></returns>
        public async Task<UpdatePanelResponseFrontDoorDto> UpdatePannelFrontDoorAsync()
        {
            return await RequestAsync<object, UpdatePanelResponseFrontDoorDto>(HttpMethod.Post, _homeAssistantEndPoints.UpdatePanelFrontDoor, HomeAssistantClient.FrontDoor);
        }

        private void AutoCreateUnitCodeForNewUnit(Reservation reservation, bool isCronJob)
        {
            try
            {

                var lastReservationCode = _dbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id).OrderByDescending(x => x.Id).FirstOrDefault();
                if (lastReservationCode != null)
                {
                    ReservationCode newReservationCode = new()
                    {
                        CreatedBy = lastReservationCode.CreatedBy,
                        LockCode = lastReservationCode.LockCode,
                        CreatedDate = lastReservationCode.CreatedDate,
                        SlotNo = lastReservationCode.SlotNo,
                        ReservationId = lastReservationCode.ReservationId,
                        Status = CodeStatus.Pending
                    };
                    _dbContext.ReservationCode.Add(newReservationCode);
                    _dbContext.SaveChanges();

                    SetReservationCode(reservation, newReservationCode, "create", isCronJob);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<VerifyFrontDoorCodeDto> VerifyCodeForFrontDoorAsync(Reservation reservation, ReservationCode reservationCode, DateTime currentEstTime)
        {
            try
            {
                VerifyFrontDoorCodeDto verifyFrontDoorCodeDto = new VerifyFrontDoorCodeDto();

                _loggerRepo.Add(LogType.Information, $"Verifying front door code for cron job at {currentEstTime} in EST.", reservation.Id);
                _loggerRepo.Add(LogType.Information, $"Verifying front door code - Getting user front door info for cron job at {currentEstTime} in EST. UnitId: {reservation.BuildingUnit?.UnitId}, Code: {reservation.Code}", null);

                var existingUserFrontDoor = await GetExistingUserFrontDoorAsync(reservation.BuildingUnit?.UnitId, reservation.Id, reservation.Code);

               // _loggerRepo.Add(LogType.Information, $"Verifying front door code - User front door info response received for cron job at {currentEstTime} in EST. UnitId: {reservation.BuildingUnit?.UnitId}, Code: {reservation.Code}, Respone -> {existingUserFrontDoor.ToJsonString()}", reservation.Id);

                if (existingUserFrontDoor != null && existingUserFrontDoor.Results.HasAny())
                {
                    foreach (var userResult in existingUserFrontDoor.Results)
                    {
                        DateTime expiresOnDate = userResult.ExpiresOn.Date;
                        DateTime expiresOnDateInEST = expiresOnDate.ToESTDateTime();

                        if (expiresOnDateInEST != reservation.EndDate.Date)
                        {
                            verifyFrontDoorCodeDto.ExpiresOn = expiresOnDateInEST;
                            _loggerRepo.Add(LogType.Information, $"Verifying front door code for cron job at {currentEstTime} in EST - Date is not accurate for UnitId: {reservation.BuildingUnit?.UnitId}, Code: {reservation.Code}.", reservation.Id);
                        }

                        string url = _homeAssistantEndPoints.GetVerifyCodeForFrontDoor(userResult.Id);

                        _loggerRepo.Add(LogType.Information, $"verify code for front door URL : {url}.", reservation.Id);
                      //  _loggerRepo.Add(LogType.Information, $"Sending request for verify code for front door URL : {url}", reservation.Id);

                        var response = await RequestAsync<object, VerifyCodeResponseFrontDoor>(HttpMethod.Get, url, HomeAssistantClient.FrontDoor);
                        _loggerRepo.Add(LogType.Information, $"verify code for front door GET response : {response.ToJsonString()}", reservation.Id);

                        if (response != null && response.Results.HasAny())
                        {
                            foreach (var result in response.Results)
                            {
                                if (result.PinNumber.ToString() != reservationCode.LockCode)
                                {
                                    verifyFrontDoorCodeDto.PinNumber = result.PinNumber.ToString();
                                    _loggerRepo.Add(LogType.Information, $"Verifying front door code for cron job at {currentEstTime} in EST - Lock code is not accurate for UnitId: {reservation.BuildingUnit?.UnitId}, Code: {reservation.Code}.", reservation.Id);
                                }
                            }
                        }
                    }
                }
                else
                {
                    _loggerRepo.Add(LogType.Information, $"existing user is not found for front door.", reservation.Id);
                }

                return verifyFrontDoorCodeDto;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void VerifyCodeForUnit(ReservationCode reservationCode, DateTime currentEstTime, ref List<MissingUnitCodeDto> missingCodesList, Reservation reservation)
        {
            try
            {
                string unit = reservation.BuildingUnit?.UnitId;
                _loggerRepo.Add(LogType.Information, $"Verifying unit code for cron job at {currentEstTime} in EST for Unit: {unit}, Code: {reservationCode.LockCode}, Slot: {reservationCode.SlotNo}.", reservation.Id);
                bool isExists = _dbContext.UnitSlotLog.Any(x => x.Unit == unit && x.Code == reservationCode.LockCode);
                //bool isExists = _dbContext.UnitSlotLog.Any(x => x.Unit == unit && x.Code == reservationCode.LockCode && x.Slot == reservationCode.SlotNo.ToString());
                if (!isExists)
                {
                    _loggerRepo.Add(LogType.Information, $"Cron Job at {currentEstTime} in EST -> Unit code is missing in UnitSlotLog table for Unit: {unit}, Code: {reservationCode.LockCode}, Slot: {reservationCode.SlotNo}.", reservation.Id);
                    MissingUnitCodeDto missingUnitCode = new MissingUnitCodeDto
                    {
                        ReservationId = reservation.Id,
                        ReservationCode = reservation.Code,
                        LockCode = reservationCode.LockCode,
                        SlotNo = reservationCode.SlotNo,
                        Unit = unit,
                        StartDate = reservation.StartDate,
                        EndDate = reservation.EndDate,
                    };
                    missingCodesList.Add(missingUnitCode);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteExpiredFrontDoorCodes()
        {
           await RequestAsync<object, object>(HttpMethod.Post, _homeAssistantEndPoints.DeleteExpiredFrontDoor, HomeAssistantClient.Unit);
        }

        #endregion

        #region Job Methods

        //public async Task ExecuteActiveCodesForUnitAsync()
        //{
        //    try
        //    {
        //        DateTime currentEstTime = Utilities.GetCurrentTimeInEST();
        //        List<int> reservationCodesToDelete = await AddRemoveActiveCodesFromCronJob(currentEstTime);
        //    }
        //    catch (Exception ex)
        //    {
        //        _loggerRepo.Add(LogType.Error, $"Error occured during ExecuteActiveCodesForUnitAsync in cron job. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", ex.StackTrace);
        //    }
        //}

        public async Task ExecuteFailedCodesForUnitAsync()
        {
            try
            {

                DateTime currentEstTime = Utilities.GetCurrentTimeInEST();
                _loggerRepo.Add(LogType.Information, $"Started RetryFailedCodesForUnit cron task at {currentEstTime} in EST.", null);
                _loggerRepo.Add(LogType.Information, $"Fetching failed Reservation codes from database for cron job at {currentEstTime} in EST.", null);

                // get active faild and deleted faild codes from database
                var reservationCodes = await _dbContext.ReservationCode.Include(x => x.Reservation).ThenInclude(x => x.BuildingUnit).ThenInclude(x => x.Building)
                    .Where(x => x.Reservation.BuildingUnit.Building.BuildingLockSystem == BuildingLockSystem.Arches
                                && x.Status == CodeStatus.ActiveFailed || x.Status == CodeStatus.DeleteFailed).ToListAsync();

                _loggerRepo.Add(LogType.Information, $"{reservationCodes.Count} Failed reservation codes fetched from database for cron job at {currentEstTime} in EST.", null);

                if (reservationCodes != null && reservationCodes.Any())
                {
                    // get distinct reservation ids from reservationCodes
                    IEnumerable<int> reservationIds = reservationCodes.Select(x => x.ReservationId).Distinct();

                    // get reservations against reservation ids
                    var reservations = await _dbContext.Reservation.Where(x => reservationIds.Contains(x.Id)).ToListAsync();

                    foreach (var reservation in reservations)
                    {
                        // get matched reservation codes
                        var matchedCodes = reservationCodes.Where(x => x.ReservationId == reservation.Id).ToList();
                        foreach (var matchedCode in matchedCodes)
                        {
                            if (matchedCode.Status == CodeStatus.ActiveFailed)
                            {
                                // it will set code in home assistant if code status is active failed
                                SetCodesForUnit(reservation, matchedCodes, true);
                            }
                            else
                            {
                                // it will remove code in home assistant if code status is deleted failed
                                DeleteCodesForUnit(reservation, matchedCodes, true);
                            }
                        }
                    }
                }

                _loggerRepo.Add(LogType.Information, $"Completed RetryFailedCodesForUnit cron task at {currentEstTime} in EST.", null);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occured during ExecuteFailedCodesForUnitAsync in cron job. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", null, ex.StackTrace);
            }
        }

        public async Task ExecuteVerificationCodesAsync()
        {
            try
            {
                DateTime currentEstTime = Utilities.GetCurrentTimeInEST();

                _loggerRepo.Add(LogType.Information, $"Started VerifyCodes cron task at {currentEstTime} in EST.", null);
                _loggerRepo.Add(LogType.Information, $"Getting active reservations to varify front door and unit codes for cron job at {currentEstTime} in EST.", null);

                var activeArchesReservations = _dbContext.Reservation.Include(x => x.BuildingUnit).ThenInclude(x => x.Building)
                                                                     .Where(x => x.BuildingUnit.Building.BuildingLockSystem == BuildingLockSystem.Arches
                                                                       && !x.Cancelled && !x.IsArchesNotificationSent
                                                                       && currentEstTime >= (x.EarlyCheckIn != null ? x.EarlyCheckIn : x.StartDate).Value
                                                                       && currentEstTime <= (x.LateCheckOut != null ? x.LateCheckOut : x.EndDate).Value).ToList();

                if (activeArchesReservations.HasAny())
                {
                    string reservationsIds = string.Join(",", activeArchesReservations.Select(x => x.Id.ToString()));
                    _loggerRepo.Add(LogType.Information, $"{activeArchesReservations.Count}- ids -> ({reservationsIds}) reservations found to varify front door and unit codes for cron job at {currentEstTime} in EST.", null);

                    List<MissingUnitCodeDto> missingUnitCodesList = new List<MissingUnitCodeDto>();
                    List<VerifyFrontDoorCodeDto> verifyFrontDoorCodesList = new List<VerifyFrontDoorCodeDto>();

                    string activeCodesEmailHtml = "<table border='1px'><tr><th>Reservation Id</th><th>Reservation Code</th><th>Building Unit</th><th>Lock Code</th><th>Slot No</th><th>Start Date</th><th>End Date</th></tr>";

                    foreach (var reservation in activeArchesReservations)
                    {
                        _loggerRepo.Add(LogType.Information, $"Reservation : {reservation.ToJsonString()} going to be varify for front door and unit codes for cron job at {currentEstTime} in EST.", reservation.Id);

                        // get all reservation code against reservation
                        var reservationCodes = _dbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id).ToList();

                        _loggerRepo.Add(LogType.Information, $"reservation codes found: {reservationCodes.ToJsonString()}. Against reservation : {reservation.ToJsonString()}. going to be verify for front door and unit codes for cron job at {currentEstTime} in EST.", reservation.Id);

                        if (reservationCodes.HasAny())
                        {
                            foreach (var reservationCode in reservationCodes)
                            {
                                try
                                {
                                    VerifyFrontDoorCodeDto verifyFrontDoorCodeDto = await VerifyCodeForFrontDoorAsync(reservation, reservationCode, currentEstTime);
                                    if (verifyFrontDoorCodeDto.ExpiresOn != null || !string.IsNullOrEmpty(verifyFrontDoorCodeDto.PinNumber))
                                    {
                                        verifyFrontDoorCodeDto.ReservationId = reservationCode.ReservationId;
                                        verifyFrontDoorCodeDto.Unit = reservation.BuildingUnit?.UnitId;
                                        verifyFrontDoorCodeDto.LockCode = reservationCode.LockCode;
                                        verifyFrontDoorCodeDto.SlotNo = reservationCode.SlotNo;
                                        verifyFrontDoorCodeDto.EndDate = reservation.EndDate;
                                        verifyFrontDoorCodeDto.ReservationCode = reservation.Code;
                                        verifyFrontDoorCodesList.Add(verifyFrontDoorCodeDto);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _loggerRepo.Add(LogType.Error, $"Error occur while verifying front door code for reservation code : {reservationCode.ToJsonString()} in cron job. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", reservation.Id, ex.StackTrace);
                                }

                                try
                                {
                                    VerifyCodeForUnit(reservationCode, currentEstTime, ref missingUnitCodesList, reservation);
                                }
                                catch (Exception ex)
                                {
                                    _loggerRepo.Add(LogType.Error, $"Error occur while verifying unit code for reservation code : {reservationCode.ToJsonString()} in cron job. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", reservation.Id, ex.StackTrace);
                                }

                                if (reservationCode.Status == CodeStatus.Active)
                                    activeCodesEmailHtml += $"<tr><td>{reservationCode.ReservationId}</td><td>{reservation.Code}</td><td>{reservation.BuildingUnit?.UnitId}</td><td>{reservationCode.LockCode}</td><td>{reservationCode.SlotNo}</td><td>{reservation.StartDate}</td><td>{reservation.EndDate}</td></tr>";
                            }
                        }
                    }

                    try
                    {
                        //set the codes for the missing unit codes
                        _loggerRepo.Add(LogType.Information, $"Setting unit missing unit codes. (ExecuteVerificationCodesAsync). At {currentEstTime} in EST." +
                            $" missingUnitCodesList: {missingUnitCodesList.ToJsonString()}", null);

                        ExecuteCommandsForUnitFromListDto(missingUnitCodesList);

                        _loggerRepo.Add(LogType.Information, $"Successfully called to set unit missing unit codes. (ExecuteVerificationCodesAsync). At {currentEstTime} in EST.", null);

                    }
                    catch(Exception ex)
                    {
                        _loggerRepo.Add(LogType.Error, $"Error occurred in setting unit missing unit codes. Error occurred during ExecuteVerificationCodesAsync in cron job. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", null, ex.StackTrace);
                    }

                    // Send email notification for Reservations With Mismatch End Dates after verification process
                    await SendMismatchDatesEmailAsync(verifyFrontDoorCodesList, currentEstTime);

                    // Send email notification for Reservations With Mismatch Lock Codes after verification process
                    await SendMismatchLockCodesEmailAsync(verifyFrontDoorCodesList, currentEstTime);

                    // Send email notification for Reservations With Missing Unit Codes in UnitSlotLog table after verification process
                    await SendMissingUnitCodesEmailAsync(missingUnitCodesList, currentEstTime);

                    activeCodesEmailHtml += "</table>";

                    // send all active codes via email in the form of table
                    _loggerRepo.Add(LogType.Information, $"Started SendActiveCodesEmail cron task at {currentEstTime} in EST for html: {activeCodesEmailHtml}.", null);
                    await SendActiveCodesEmailAsync(activeCodesEmailHtml, currentEstTime);
                    _loggerRepo.Add(LogType.Information, $"Completed SendActiveCodesEmail cron task at {currentEstTime} in EST for html: {activeCodesEmailHtml}.", null);

                    activeArchesReservations.ForEach(x => x.IsArchesNotificationSent = true);
                    _dbContext.SaveChanges();
                }

                _loggerRepo.Add(LogType.Information, $"Completed VerifyCodes cron task at {currentEstTime} in EST.", null);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred during ExecuteVerificationCodesAsync in cron job. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", null, ex.StackTrace);
            }
        }

        public void ExecuteCommandsForUnitFromListDto(List<MissingUnitCodeDto> missingUnitCodesList)
        {
            foreach (MissingUnitCodeDto missingUnitCodeDto in missingUnitCodesList)
            {
                CommandDto unitCommandDto = new()
                {
                    action = "add",
                    code = missingUnitCodeDto.LockCode,
                    from = missingUnitCodeDto.StartDate,
                    to = missingUnitCodeDto.EndDate,
                    //newReservation = ,
                    //newUnit =,
                    reservation = missingUnitCodeDto.ReservationId.ToString(),
                    slot = missingUnitCodeDto.SlotNo.ToString(),
                    unit = missingUnitCodeDto.Unit,
                };

                //_homeAssistantService.AdjustCommmandDtoDatesTmeOfDay(commandDto);
                ExecuteCommandsForUnitAsync(unitCommandDto, missingUnitCodeDto.ReservationId).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// send email notification from unitlogs for all unit which battery level is below than 40%.
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteLowBatteryLevelEmailNotificationAsync()
        {
            try
            {
                DateTime currentEstTime = Utilities.GetCurrentTimeInEST();
                _loggerRepo.Add(LogType.Information, $"Started LowBatteryLevelEmailNotification cron task at {currentEstTime} in EST.", null);
                _loggerRepo.Add(LogType.Information, $"Fetching records from unitlog entity for low battery level which battery level is below than 40% at {currentEstTime} in EST.", null);
                var lowBatteryLevelUnitLogs = _dbContext.UnitLog.Where(x => Convert.ToInt32(x.BatteryLevel.Trim()) < 40).ToList();
                _loggerRepo.Add(LogType.Information, $"Fetched unitlog records which battery level is below than 40% at {currentEstTime} in EST. Records -> {lowBatteryLevelUnitLogs.ToJsonString()}", null);

                if (lowBatteryLevelUnitLogs.HasAny())
                {
                    string message = "<table border='1px'><tr><th>Building Unit</th><th>Battery Level</th><th>Last Update</th></tr>";
                    lowBatteryLevelUnitLogs.ForEach(x =>
                    {
                        message += $"<tr><td>{x.Unit}</td><td>{x.BatteryLevel}</td><td>{x.LastUpdatedTime.ToString()}</td></tr>";
                    });
                    message += "</table>";
                    _loggerRepo.Add(LogType.Information, $"sending email notification for low battery level at {currentEstTime} in EST. Email Message : {message}", null);
                    await SendEmailMessageAsync($"Low battery level units.", message);
                }

                _loggerRepo.Add(LogType.Information, $"Completed LowBatteryLevelEmailNotification cron task at {currentEstTime} in EST.", null);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occured during ExecuteLowBatteryLevelEmailNotificationAsync in cron job. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", null, ex.StackTrace);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Send email notification for Reservations With Mismatch End Dates after verification process
        /// </summary>
        /// <param name="verifyFrontDoorCodesList"></param>
        /// <param name="currentEstTime"></param>
        /// <returns></returns>
        private async Task SendMismatchDatesEmailAsync(List<VerifyFrontDoorCodeDto> verifyFrontDoorCodesList, DateTime currentEstTime)
        {
            try
            {
                var mistmatchDatesList = verifyFrontDoorCodesList.Where(x => x.ExpiresOn != null).ToList();
                if (mistmatchDatesList.HasAny())
                {
                    string mismatchDatesEmailHtml = "<table border='1px'><tr><th>Reservation Id</th><th>Reservation Code</th><th>Current Date</th><th>Actual Date</th><th>Building Unit</th><th>Lock Code</th><th>Slot No</th></tr>";
                    mistmatchDatesList.ForEach(x =>
                    {
                        mismatchDatesEmailHtml += $"<tr><td>{x.ReservationId}</td><td>{x.ReservationCode}</td><td>{x.EndDate}</td><td>{x.ExpiresOn}</td><td>{x.Unit}</td><td>{x.LockCode}</td><td>{x.SlotNo}</td></tr>";
                    });
                    mismatchDatesEmailHtml += "</table>";

                    _loggerRepo.Add(LogType.Information, $"Sending email notification for Reservations With Mismatch End Dates at {currentEstTime} in EST. Email Message : {mismatchDatesEmailHtml}", null);
                    await SendEmailMessageAsync($"Reservations With Mismatch End Dates", mismatchDatesEmailHtml);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Send email notification for Reservations With Mismatch Lock Codes after verification process
        /// </summary>
        /// <param name="verifyFrontDoorCodesList"></param>
        /// <param name="currentEstTime"></param>
        /// <returns></returns>
        private async Task SendMismatchLockCodesEmailAsync(List<VerifyFrontDoorCodeDto> verifyFrontDoorCodesList, DateTime currentEstTime)
        {
            try
            {
                var mistmatchLockCodesList = verifyFrontDoorCodesList.Where(x => !string.IsNullOrEmpty(x.PinNumber)).ToList();
                if (mistmatchLockCodesList.HasAny())
                {
                    string mismatchLockCodesEmailHtml = "<table border='1px'><tr><th>Reservation Id</th><th>Reservation Code</th><th>Building Unit</th><th>Current Lock Code</th><th>Actual Lock Code</th><th>Slot No</th></tr>";
                    mistmatchLockCodesList.ForEach(x =>
                    {
                        mismatchLockCodesEmailHtml += $"<tr><td>{x.ReservationId}</td><td>{x.ReservationCode}</td><td>{x.Unit}</td><td>{x.LockCode}</td><td>{x.PinNumber}</td><td>{x.SlotNo}</td></tr>";
                    });
                    mismatchLockCodesEmailHtml += "</table>";

                    _loggerRepo.Add(LogType.Information, $"Sending email notification for Reservations With Mismatch Lock Codes at {currentEstTime} in EST. Email Message : {mismatchLockCodesEmailHtml}", null);
                    await SendEmailMessageAsync($"Reservations With Mismatch Lock Codes", mismatchLockCodesEmailHtml);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Send email notification for Reservations With Missing Unit Codes in UnitSlotLog table after verification process
        /// </summary>
        /// <param name="missingUnitCodesList"></param>
        /// <param name="currentEstTime"></param>
        /// <returns></returns>
        private async Task SendMissingUnitCodesEmailAsync(List<MissingUnitCodeDto> missingUnitCodesList, DateTime currentEstTime)
        {
            try
            {
                if (missingUnitCodesList.HasAny())
                {
                    string missingCodesEmailHtml = "<table border='1px'><tr><th>Reservation Id</th><th>Reservation Code</th><th>Building Unit</th><th>Lock Code</th><th>Slot No</th><th>Start Date</th><th>End Date</th></tr>";
                    missingUnitCodesList.ForEach(x =>
                    {
                        missingCodesEmailHtml += $"<tr><td>{x.ReservationId}</td><td>{x.ReservationCode}</td><td>{x.Unit}</td><td>{x.LockCode}</td><td>{x.SlotNo}</td><td>{x.StartDate}</td><td>{x.EndDate}</td></tr>";
                    });
                    missingCodesEmailHtml += "</table>";

                    _loggerRepo.Add(LogType.Information, $"Sending email notification for Missing Unit Codes in UnitSlotLog table at {currentEstTime} in EST. Email Message : {missingCodesEmailHtml}", null);
                    await SendEmailMessageAsync($"Missing Unit Codes in UnitSlotLog table", missingCodesEmailHtml);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// send all active codes via email in the form of table at 4:00 PM in EST time
        /// </summary>
        /// <param name="emailMessage"></param>
        /// <param name="currentEstTime"></param>
        /// <returns></returns>
        private async Task SendActiveCodesEmailAsync(string emailMessage, DateTime currentEstTime)
        {
            try
            {
                string activeCodesEmailJobTime = _configuration["ActiveCodesEmailJobTime"];
                DateTime activeCodesEmailJobDateTime = DateTimeExtension.GetDateTime(currentEstTime, activeCodesEmailJobTime);

                if (currentEstTime.Hour == activeCodesEmailJobDateTime.Hour)
                {
                    List<string> recipients = _configuration["ActiveCodesEmailRecipients"].Split(",").ToList();

                    _loggerRepo.Add(LogType.Information, $"Sending ActiveCodes email for recipients: {recipients.ToJsonString()} in cron job at {currentEstTime} in EST.", null);
                    await SendEmailMessageAsync($"Active Codes", emailMessage, recipients);
                    _loggerRepo.Add(LogType.Information, $"Sent ActiveCodes email for recipients: {recipients.ToJsonString()} successfully in cron job at {currentEstTime} in EST.", null);
                }
                else
                {
                    _loggerRepo.Add(LogType.Information, $"Time did not match for SendActiveCodesEmail cron task at {currentEstTime} in EST. Current Hour: {currentEstTime.Hour}, ActiveCodesEmailJobDateTime Hour: {activeCodesEmailJobDateTime.Hour}", null);
                }
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occured during SendActiveCodesEmail cron task at {currentEstTime} in EST. Error : {ex.Message}, InnerException: {ex.InnerException?.Message}.", null, ex.StackTrace);
            }
        }

        private void SaveFrontDoorUserId(int reservationId, int frontDoorUserId)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    var dbReservation = dbContext.Reservation.FirstOrDefault(x => x.Id == reservationId);
                    dbReservation.FrontDoorUserId = frontDoorUserId;
                    dbContext.Entry(dbReservation).State = EntityState.Modified;
                    dbContext.SaveChanges();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //private async Task<List<int>> AddRemoveActiveCodesFromCronJob(DateTime currentEstTime)
        //{
        //    try
        //    {
        //        _loggerRepo.Add(LogType.Information, $"Started AddRemoveActiveCodes cron task at {currentEstTime} in EST.");
        //        _loggerRepo.Add(LogType.Information, $"Fetching active reservations from database for cron job at {currentEstTime} in EST.");

        //        // get reservations that are going to start in next 4 hours using EST timezone
        //        // OR
        //        // have expired during last 4 hours using EST timezone
        //        var reservations = await _dbContext.Reservation.Where(x => ((x.EarlyCheckIn != null ? x.EarlyCheckIn : x.StartDate).Value <= currentEstTime.AddHours(4)
        //                                                                && (x.EarlyCheckIn != null ? x.EarlyCheckIn : x.StartDate).Value >= currentEstTime)
        //                                                                || ((x.LateCheckOut != null ? x.LateCheckOut : x.EndDate).Value >= currentEstTime.AddHours(-4)
        //                                                                && (x.LateCheckOut != null ? x.LateCheckOut : x.EndDate).Value <= currentEstTime)).ToListAsync();

        //        string reservationIds = string.Join(",", reservations.Select(x => x.Id));
        //        _loggerRepo.Add(LogType.Information, $"Reservations with ids : {reservationIds} fetched from database for cron job at {currentEstTime} in EST.");

        //        List<int> reservationCodesToDelete = new();

        //        foreach (var reservation in reservations)
        //        {
        //            DateTime fromDate = reservation.FromDate();
        //            DateTime toDate = reservation.ToDate();

        //            double fromHourDiff = Math.Floor((fromDate - currentEstTime).TotalHours);
        //            double toHourDiff = Math.Ceiling((toDate - currentEstTime).TotalHours);

        //            _loggerRepo.Add(LogType.Information, $"Analyzing reservation with id : {reservation.Id}, fromHourDiff : {fromHourDiff}, toHourDiff : {toHourDiff} to add/delete code for cron job at {currentEstTime} in EST.");

        //            // get all reservation code against reservation
        //            var reservationCodes = await _dbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id).ToListAsync();

        //            if (fromHourDiff == 3 || fromHourDiff == 4)
        //            {
        //                _loggerRepo.Add(LogType.Information, $"Setting codes for reservation with id : {reservation.Id} for cron job at {currentEstTime} in EST.");

        //                // set code in Home Assistant before 4 hour of checkin
        //                SetCodesForUnit(reservation, reservationCodes, true);
        //            }
        //            else if (toHourDiff == 0 || toHourDiff == -1)
        //            {
        //                _loggerRepo.Add(LogType.Information, $"Deleting codes for reservation with id : {reservation.Id} for cron job at {currentEstTime} in EST.");

        //                var idsToDelete = reservationCodes.Where(x => x.Status == CodeStatus.Active || x.Status == CodeStatus.DeleteFailed).Select(x => x.Id);
        //                if (idsToDelete.HasAny())
        //                    reservationCodesToDelete.AddRange(idsToDelete);

        //                // remove code from Home Assistant after checkout
        //                DeleteCodesForUnit(reservation, reservationCodes, true, true);
        //            }
        //        }

        //        _loggerRepo.Add(LogType.Information, $"Completed AddRemoveActiveCodes cron task at {currentEstTime} in EST.");

        //        return reservationCodesToDelete;
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        private void SetReservationCode(Reservation reservation, ReservationCode reservationCode, string action, bool isCronJob = false, bool deleteFrontDoorCode = false)
        {
            if (reservation.BuildingUnit is null)
                reservation.BuildingUnit = _dbContext.BuildingUnit.FirstOrDefault(x => x.Id == reservation.BuildingUnitId);

            DateTime currentEstTime = Utilities.GetCurrentTimeInEST();

            CommandDto unitCommandDto = new()
            {
                action = action,
                from = _dateRepo.GetFromDate(reservation, BuildingLockSystem.Arches, reservation.Id, currentEstTime, isCronJob, false),
                to = _dateRepo.GetToDate(reservation, isCronJob, currentEstTime, true),
                unit = reservation?.BuildingUnit?.UnitId.ToLower(),
                reservation = reservation.Code,
                code = reservationCode.LockCode,
                slot = reservationCode.SlotNo.ToString()
            };

            try
            {

                if (isCronJob)
                {
                    _loggerRepo.Add(LogType.Information, $"Automate - Setting device code at {currentEstTime} in EST for UnitCommand: {unitCommandDto.ToJsonString()}.", reservation.Id);

                    Thread.Sleep(30000);

                    // set device codes using home assistant api
                    StartProcessForUnit(unitCommandDto, action, reservation, reservationCode, currentEstTime, true, deleteFrontDoorCode);
                }
                else
                {
                    _loggerRepo.Add(LogType.Information, $"Setting device code at {currentEstTime} in EST for UnitCommand: {unitCommandDto.ToJsonString()}.", reservation.Id);

                    Task.Run(() =>
                    {
                        _millisecond += 30000;
                        Thread.Sleep(_millisecond);
                        StartProcessForUnit(unitCommandDto, action, reservation, reservationCode, currentEstTime, false, deleteFrontDoorCode);
                    });
                }
            }
            catch (Exception)
            {
                // in case of exception occurred in HomeAssistant api
                throw new Exception($"Error setting code(s) on HomeAssistant.");
            }
        }

        private bool StartProcessForUnit(CommandDto unitCommandDto, string action, Reservation reservation, ReservationCode reservationCode, DateTime currentEstTime, bool isCronJob, bool deleteFrontDoorCode)
        {
            try
            {
                //if (action == "delete")
                //{
                //    if (deleteFrontDoorCode)
                //        DeleteCodeForFrontDoorAsync(reservation, isCronJob, currentEstTime).GetAwaiter().GetResult();
                //}
                //else
                //{
                //    CreateCodeForFrontDoorAsync(reservation, reservationCode, isCronJob, currentEstTime).GetAwaiter().GetResult();
                //}

                // set unit codes using home assistant api
                bool unitSuccess = ExecuteCommandsForUnitAsync(unitCommandDto, reservation.Id).GetAwaiter().GetResult();

                if (!unitSuccess)
                {
                    // retry sending/deleting code
                    unitSuccess = ExecuteCommandsForUnitAsync(unitCommandDto, reservation.Id).GetAwaiter().GetResult();
                }

                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    string logMessage = isCronJob ? "Automate - " : string.Empty;

                    // update reservation code status in database
                    var reservationCodeToUpdate = dbContext.ReservationCode.FirstOrDefault(x => x.Id == reservationCode.Id);
                    if (reservationCodeToUpdate != null)
                    {
                        if (unitSuccess)
                            reservationCodeToUpdate.Status = action == "delete" ? CodeStatus.Deleted : CodeStatus.Active;
                        else
                            reservationCodeToUpdate.Status = action == "delete" ? CodeStatus.DeleteFailed : CodeStatus.ActiveFailed;

                        // update reservation code
                        reservationCodeToUpdate.ModifiedDate = DateTime.UtcNow;
                        dbContext.Entry(reservationCodeToUpdate).State = EntityState.Modified;
                        logMessage += $"Reservation code status updated at {currentEstTime} in EST for Command: {unitCommandDto.ToJsonString()}.";
                    }
                    else
                    {
                        logMessage += $"No reservation code found to update status at {currentEstTime} in EST for Command: {unitCommandDto.ToJsonString()}.";
                    }

                    // add logger info
                    dbContext.Logger.Add(new DAL.DomainEntities.Logger
                    {
                        CreatedDate = DateTime.UtcNow,
                        LogTypeId = LogType.Information,
                        Message = logMessage,
                        ReservationId = reservation.Id,
                    });

                    // save changes to database
                    dbContext.SaveChanges();
                }

                return unitSuccess;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task<TResult> RequestAsync<T, TResult>(HttpMethod requestMethod, string url, HomeAssistantClient homeAssistantClient, T model = null) where T : class, new()
        {
            try
            {
                var client = GetHttpClient(homeAssistantClient);

                if (model is null) model = new();
                HttpResponseMessage response = new();
                HttpContent content = GetSerializeObject(model);

                if (requestMethod == HttpMethod.Get)
                    response = await client.GetAsync(url);
                else if (requestMethod == HttpMethod.Post)
                    response = await client.PostAsync(url, content);
                else if (requestMethod == HttpMethod.Put)
                    response = await client.PutAsync(url, content);
                else if (requestMethod == HttpMethod.Delete)
                    response = await client.DeleteAsync(url);

                return await ReadResponse<TResult>(response);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task<HttpResponseMessage> PostRequestForUnitAsync<T>(string endPointUrl, T model, HomeAssistantClient homeAssistantClient) where T : class
        {
            try
            {
                var client = GetHttpClient(homeAssistantClient);
                HttpContent content = GetSerializeObject(model);
                return await client.PostAsync(endPointUrl, content);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static HttpClient GetHttpClient(HomeAssistantClient apiClientBaseAddress)
        {
            try
            {


                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                System.Security.Cryptography.X509Certificates.X509Chain chain,
                System.Net.Security.SslPolicyErrors sslPolicyErrors)
                { return true; };



                //System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                //delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                //System.Security.Cryptography.X509Certificates.X509Chain chain,
                //System.Net.Security.SslPolicyErrors sslPolicyErrors)
                //{ return true; };


                HttpClientHandler clientHandler = new()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    SslProtocols = System.Security.Authentication.SslProtocols.None,
                };

                HttpClient client = new(clientHandler);
                switch (apiClientBaseAddress)
                {
                    case HomeAssistantClient.Unit:
                        {
                            client.BaseAddress = new Uri("https://r7pb0zjthmknp9qbucxp07vykjqb2670.ui.nabu.casa");
                            break;
                        }
                    case HomeAssistantClient.FrontDoor:
                        {
                            client.BaseAddress = new Uri("https://100.12.159.106:11001");
                        }
                        break;
                    default:
                        throw new NotImplementedException("default client is not set !!");
                }
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("NODE_TLS_REJECT_UNAUTHORIZED", "0");
                return client;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetUnitId(int buildingUnitId)
        {
            try
            {
                string unitId = string.Empty;
                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    var buildingUnit = dbContext.BuildingUnit.FirstOrDefault(x => x.Id == buildingUnitId);
                    if (buildingUnit != null)
                        unitId = buildingUnit.UnitId;
                }
                return unitId;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static HttpContent GetSerializeObject(object model)
        {
            string serializedString = JsonConvert.SerializeObject(model);
            HttpContent stringContent = new StringContent(serializedString, Encoding.UTF8, MediaTypeNames.Application.Json);
            return stringContent;
        }

        private static async Task<T> ReadResponse<T>(HttpResponseMessage response)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private List<Result> GetExistingUserMatchedResultWithReservation(Reservation oldReservation, ExistingUserResponseFrontDoorDto existingUserFrontDoor)
        {
            string unitId = GetUnitId(oldReservation.BuildingUnitId);
            return existingUserFrontDoor.Results.Where(x => x.FirstName.Equals($"GLS {unitId}") && x.LastName.Equals(oldReservation.Code) &&
                                                                                 x.StartedOn.Equals(oldReservation.FromDate()) && x.ExpiresOn.Equals(oldReservation.ToDate())).ToList();
        }

        public async Task SendEmailMessageAsync(string subject, string message, List<string> recipients = null, int? reservationId = null)
        {
            try
            {
                bool isSuccess = await _emailService.SendAsync(new EmailMessage() { Subject = subject, Message = message }, recipients, reservationId);
                if (isSuccess)
                    _logRepo.LogMessage($"Send Email Successfully. subject : {subject} - message : {message}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);
                else
                    _logRepo.LogMessage($"Email sending failed. subject : {subject} - message : {message}.", null, true, Utilities.GetCurrentTimeInEST(), LogType.Warning);
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Email sending failed. subject : {subject} - message : {message}. Error Message: {ex.Message} - InnerException : {ex.InnerException} - StackTrace : {ex.StackTrace}", null, true, DateTime.UtcNow, LogType.Error);
            }
        }

        #endregion
    }
}