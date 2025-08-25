using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request;
using StayAccess.DTO.Request.MC;
using StayAccess.DTO.ReservationCode;
using StayAccess.DTO.Reservations;
using StayAccess.DTO.Responses.Latch;
using StayAccess.DTO.Responses.Logger;
using StayAccess.Latch.Interfaces;
using StayAccess.MC.Interfaces;
using StayAccess.MC.Repositories;
using StayAccess.Tools;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace StayAccess.BLL.Repositories
{
    public class ReservationService : IReservationService
    {
        private readonly IConfiguration _configuration;
        private readonly IGenericService<DAL.DomainEntities.Reservation> _reservationRepo;
        private readonly IGenericService<ReservationLatchData> _reservationLatchDataRepo;
        private readonly IGenericService<ReservationMCData> _reservationMCDataRepo;
        private readonly IGenericService<CodeTransaction> _codeTransactionRepo;
        private readonly IReservationCodeService _reservationCodeRepo;
        private readonly IHomeAssistantService _homeAssistantRepo;
        private readonly ILatchService _latchRepo;
        private readonly ICodeTransactionService _codeTransactionService;
        private readonly ILoggerService<ReservationService> _loggerService;
        private readonly IDateService _dateRepo;
        private readonly IBuildingLockSystemService _buildingLockSystemRepo;
        private readonly IGenericService<Logger> _loggerRepo;
        private readonly IGenericService<BuildingUnit> _buildingUnitRepo;
        private readonly StayAccessDbContext _stayAccessDbContext;
        private readonly IUsedBackupCodesService _usedBackupCodesService;
        private readonly IMCService _mcService;

        public ReservationService(IConfiguration configuration, IGenericService<Reservation> reservationRepo, IGenericService<CodeTransaction> codeTransactionRepo,
            IReservationCodeService reservationCodeRepo, IHomeAssistantService homeAssistantRepo, ILatchService latchRepo, ICodeTransactionService codeTransactionService,
            ILoggerService<ReservationService> loggerService, IDateService dateRepo, IBuildingLockSystemService buildingLockSystemRepo,
            IGenericService<Logger> loggerRepo, IGenericService<BuildingUnit> buildingUnitRepo,
            IGenericService<ReservationLatchData> reservationLatchDataRepo, IGenericService<ReservationMCData> reservationMCDataRepo,
            StayAccessDbContext stayAccessDbContext, IUsedBackupCodesService usedBackupCodesService, IMCService mcService)
        {
            _configuration = configuration;
            _reservationRepo = reservationRepo;
            _reservationLatchDataRepo = reservationLatchDataRepo;
            _reservationMCDataRepo = reservationMCDataRepo;
            _reservationCodeRepo = reservationCodeRepo;
            _codeTransactionRepo = codeTransactionRepo;
            _homeAssistantRepo = homeAssistantRepo;
            _latchRepo = latchRepo;
            _codeTransactionService = codeTransactionService;
            _loggerService = loggerService;
            _dateRepo = dateRepo;
            _buildingLockSystemRepo = buildingLockSystemRepo;
            _loggerRepo = loggerRepo;
            _buildingUnitRepo = buildingUnitRepo;
            _stayAccessDbContext = stayAccessDbContext;
            _usedBackupCodesService = usedBackupCodesService;
            _mcService = mcService;
        }
        public async Task UpdateLockKey()
        {
            var endpoint = "https://rest.latchaccess.com/access/sdk/v1/doors";
            var bearerToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IkQ1UTE2Z3MwbjBYeDV2NUlUcGFWRiJ9.eyJodHRwczovL3Jlc3QubGF0Y2hhY2Nlc3MuY29tL3BhcnRuZXJfdXVpZCI6IjE4MGE4OGY2LTY0YWMtNDYzMC1hN2UyLTlhYmJkYjMwZDZkMCIsImh0dHBzOi8vcmVzdC5sYXRjaGFjY2Vzcy5jb20vcGFydG5lcl9zY29wZSI6e30sImh0dHBzOi8vcmVzdC5sYXRjaGFjY2Vzcy5jb20vZ2VtaW5pX3Njb3BlIjoibTJtIiwiaXNzIjoiaHR0cHM6Ly9hdXRoLnByb2QubGF0Y2guY29tLyIsInN1YiI6InZCaUZ5NFBZaVBJY1lGSXE0NXllWWdJR3FWTGR6dEVkQGNsaWVudHMiLCJhdWQiOiJodHRwczovL3Jlc3QubGF0Y2hhY2Nlc3MuY29tL2FjY2Vzcy9zZGsiLCJpYXQiOjE3MzE1MDc3MTQsImV4cCI6MTczMTU5NDExNCwiZ3R5IjoiY2xpZW50LWNyZWRlbnRpYWxzIiwiYXpwIjoidkJpRnk0UFlpUEljWUZJcTQ1eWVZZ0lHcVZMZHp0RWQifQ.sXlLPRbn5bO7URn5NgEbPJWn9uF_DYSp-da1mB5p5GAkBjn1uB6efyMxvhLe1jGcr1qXitLUAAjv72JECpjhg4hxnUSIB8qOhpHoSAO3D4bqVLskU7W91DnWhZW-e8VWPepHl4mTamfNb0yWap-1ewiRUrVl1V69rl4QxWGWPXQ-DO_wH9vWl-agPNpYbDJwIEWfp50PmHKZHITV88AQK9UOZRSvoe5csK-6NbZAAgxLxOLxmBPonfuy0Z_jarqquPkZjN9IqucTEr1GmPYUtF-HAho4c1wsJgZlNhWmRwupFnRI76jb8SDCb0RWe44U1P3dRz__gFSgVw3Qgtcd6A";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint));
                var content = await result.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<Doors>(content);//deserialize object to  object below.then loop through the list
                                                                             //do I have to map it to a json object?
                var latchLockKeys = _stayAccessDbContext.LockKey.Include(x => x.BuildingUnit).Where(lk => lk.BuildingUnit.BuildingId == 2).ToList();
                //foreach (var lockKey in latchLockKeys)
                //{
                //    var matchingName = response.doors.FirstOrDefault(x => x.name == lockKey.BuildingUnit.UnitId);
                //    if (matchingName != null)
                //    {
                //        lockKey.UUid = matchingName.uuid;
                //        //}
                //    }
                //    _stayAccessDbContext.SaveChanges();
                //}
                foreach (var door in response.doors)
                {
                    if(door.accessibilityType == "PRIVATE")
                    {
                        var unit = latchLockKeys.FirstOrDefault(rec => rec.BuildingUnit.UnitId == door.name);
                        if (unit != null)
                        {
                            unit.UUid = door.uuid;
                        }
                    }
                    else
                    {
                        _stayAccessDbContext.LockKey.Add(new LockKey { Name = door.name, CreatedBy="Rikkik", BuildingId = 2, UUid = door.uuid, KeyId = new Guid(door.uuid) });
                    }
                    
                   
                _stayAccessDbContext.SaveChanges();
                }
            }
        }
        public async Task<ReservationCodeResponseDto> AddAsync(ReservationRequestDto reservationDto, string userName, BuildingLockSystem buildingLockSystem, bool isBackupReservation, List<ReservationCodeDto> reservationCodes = null)
        {
            try
            {


                DateTime startDateWithSetting = DateTimeExtension.GetDateTime(reservationDto.StartDate, _dateRepo.GetReservationStartTimeSetting(buildingLockSystem, reservationDto.Id, Utilities.GetCurrentTimeInEST(), false));
                DateTime endDateWithSetting = DateTimeExtension.GetDateTime(reservationDto.EndDate, _configuration["ReservationEndTime"]);

                DateTime startDate;
                DateTime endDate;

                switch (buildingLockSystem)
                {
                    case BuildingLockSystem.Latch:
                        //set the startDate to an earlier time of day then the setting, when the Dto's date is earlier than the setting time of day
                        //set the endDate to a later time of day then the setting, when the Dto's date is later than the setting time of day
                        startDate = new DateTime(Math.Min(startDateWithSetting.Ticks, reservationDto.StartDate.Ticks));
                        endDate = new DateTime(Math.Max(endDateWithSetting.Ticks, reservationDto.EndDate.Ticks));
                        break;
                    default:
                        startDate = startDateWithSetting;
                        endDate = endDateWithSetting;
                        break;
                }


                string code = reservationDto.Code;
                if (!isBackupReservation)
                {
                    ErrorIfCodeContainsBackup(code, reservationDto.Id);
                }
                Reservation reservation = new()
                {
                    Id = reservationDto.Id,
                    BuildingUnitId = reservationDto.BuildingUnitId,
                    Code = code,
                    StartDate = startDate,
                    EndDate = endDate,
                    EarlyCheckIn = reservationDto.EarlyCheckIn,
                    LateCheckOut = reservationDto.LateCheckOut,
                    Cancelled = reservationDto.Cancelled,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userName,
                    FirstName = reservationDto.FirstName, 
                    LastName = reservationDto.LastName, 
                    Email = reservationDto.Email, 
                    Phone = reservationDto.Phone
                };

                _loggerService.Add(LogType.Information, $"Creating new reservation: {JsonConvert.SerializeObject(reservation)}.", null);
                _reservationRepo.AddWithSave(reservation);
                _loggerService.Add(LogType.Information, $"New reservation created successfully for: {JsonConvert.SerializeObject(reservation)}.", reservation.Id);

                ReservationCodeResponseDto reservationCodeResponseDto = new()
                {
                    ReservationId = reservation.Id
                };
                if (buildingLockSystem == BuildingLockSystem.MC)
                {
                   HttpStatusCode apiLockSystemResponseHttpStatusCode = _mcService.CreateReservationAsync(reservation, userName).GetAwaiter().GetResult();
                }
                else 
                 reservationCodeResponseDto.ReservationCodeIds = await _reservationCodeRepo.BulkAddAsync(reservation.Id, reservationCodes, userName, buildingLockSystem);


                switch (buildingLockSystem)
                {
                    case BuildingLockSystem.Arches:
                        if (reservationCodes.HasAny())
                        {
                            if (reservation.IsCurrentActiveReservation())
                            {
                                // Set device lock codes on HomeAssistant api, if reservation is active
                                //await SetCodesAsync(reservation, null);

                                // start executing pending transactions in background
                                _codeTransactionService.ExecuteTransactions();
                            }
                        }
                        break;
                    case BuildingLockSystem.Latch:
                    case BuildingLockSystem.MC :
                        break;
                }
                return reservationCodeResponseDto;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<HttpStatusCode> AddMCReservationAsync(Reservation reservation, string userName)
        {
            
            HttpStatusCode apiLockSystemResponseHttpStatusCode = _mcService.CreateReservationAsync(reservation, userName).GetAwaiter().GetResult();
            return apiLockSystemResponseHttpStatusCode;
        }

        public async Task UpdateAsync(ReservationRequestDto reservationDto, string userName, BuildingLockSystem buildingLockSystem, bool isCronJob = false)
        {
            try
            {
                Reservation reservation = await GetByIdAsync(reservationDto.Id);
                Reservation oldReservation = reservation.DeepClone();
                if (reservation == null)
                    throw new Exception($"Reservation of id {reservationDto.Id} not found when attempting to update reservation.");

                if (reservationDto.Cancelled && !oldReservation.Cancelled && buildingLockSystem == BuildingLockSystem.MC)
                { ///add code to only run the api if the oldreservation wasn't called
                    ReservationMCData reservationMcData = GetFirstOrDefault(reservationDto.Id);//_stayAccessDbContext.ReservationMCData.Where(x => x.ReservationId == reservationDto.Id).FirstOrDefault();
                    HttpStatusCode apiLockSystemResponseHttpStatusCode = _mcService.RemoveReservationAsync(reservation, reservationMcData).GetAwaiter().GetResult();
                }
                
                int oldBuildingUnitId = oldReservation.BuildingUnitId;
                int buildingUnitId = reservationDto.GetBuildingUnitId();

                int newBuildingUnitBuildingId = _buildingUnitRepo.GetAsync(x => x.Id == buildingUnitId, x => x.Building).Result.BuildingId;
                int oldBuildingUnitBuildingId = _buildingUnitRepo.GetAsync(x => x.Id == oldBuildingUnitId, x => x.Building).Result.BuildingId;

                if (oldBuildingUnitBuildingId != newBuildingUnitBuildingId)
                    throw new Exception("Invalid building unit. New unit must be in the same building.");

                DateTime oldFromDate = oldReservation.FromDate();
                DateTime oldToDate = oldReservation.ToDate();
                String oldFirstName = oldReservation.FirstName;
                String oldLastName = oldReservation.LastName;

                DateTime startDate;

                DateTime endDate;

                switch (buildingLockSystem)
                {
                    case BuildingLockSystem.Latch:
                        //take an earlier startDate than the setting time of day, when the Dto has a date that's time of day is earlier than the setting
                        //take a later endDate than the setting time of day, when the Dto has a date that's time of day is later than the setting
                        startDate = reservationDto.StartDate != default
                            ? new DateTime(Math.Min(reservationDto.StartDate.Ticks, GetStartDate(reservationDto.StartDate, buildingLockSystem, Utilities.GetCurrentTimeInEST(), isCronJob).Ticks))
                            : oldFromDate;
                        endDate = reservationDto.EndDate != default
                            ? new DateTime(Math.Max(reservationDto.EndDate.Ticks, GetEndDate(reservationDto.EndDate).Ticks))
                            : oldToDate;
                        break;
                    default:
                        startDate = reservationDto.StartDate != default
                              ? GetStartDate(reservationDto.StartDate, buildingLockSystem, Utilities.GetCurrentTimeInEST(), isCronJob)
                              : oldFromDate;
                        endDate = reservationDto.EndDate != default
                               ? GetEndDate(reservationDto.EndDate)
                               : oldToDate;
                        break;
                }

                bool isModified = !reservationDto.BuildingUnitId.Equals(reservationDto.NewBuildingUnitId) || !reservationDto.Code.Equals(reservationDto.NewCode) || !reservation.EndDate.Equals(endDate);

                if (reservation is null)
                {
                    throw new Exception("Reservation not found.");
                }
                else
                {
                    _loggerService.Add(LogType.Information, $"Reservation found before update reservation: {JsonConvert.SerializeObject(reservation)}.", reservation.Id, string.Empty, false);
                }

                bool isArchesBackupReservation = _usedBackupCodesService.IsArchesBackupReservation(reservation.Id).Result.isValid;

                if (isArchesBackupReservation)
                {
                    reservation.Code = _usedBackupCodesService.ToCodeAsUsed(reservation.Code);
                }
                else
                {
                    string code = reservationDto.GetCode();
                    ErrorIfCodeContainsBackup(code, reservation.Id);
                    reservation.Code = code;
                }

                reservation.Id = reservationDto.Id;
                reservation.BuildingUnitId = buildingUnitId;
                reservation.Code = reservationDto.GetCode();
                reservation.StartDate = startDate;
                reservation.EndDate = endDate;
                reservation.EarlyCheckIn = reservationDto.EarlyCheckIn;
                reservation.LateCheckOut = reservationDto.LateCheckOut;
                reservation.Cancelled = reservationDto.Cancelled;
                reservation.FirstName = reservationDto.FirstName;
                reservation.LastName = reservationDto.LastName;
                reservation.ModifiedDate = DateTime.UtcNow;
                reservation.ModifiedBy = userName;

                _loggerService.Add(LogType.Information, $"Updating reservation (database update): {JsonConvert.SerializeObject(reservation)}.", reservation.Id, string.Empty, false);
                _reservationRepo.UpdateWithSave(reservation);
                _loggerService.Add(LogType.Information, $"Reservation updated successfully (database update) for: {JsonConvert.SerializeObject(reservation)}.", reservation.Id);


                if (isArchesBackupReservation)
                {
                    await _usedBackupCodesService.NotifyBackupMarkedAsUsedAsync(reservation.Id, userName);
                }

                DateTime newFromDate = reservation.FromDate();
                DateTime newToDate = reservation.ToDate();
                int newBuilingUnitId = reservation.BuildingUnitId;
                bool hasActiveCodes = _codeTransactionService.HasActiveCodes(reservation.Id);

                List<int> pendingCodesIds = _codeTransactionService.GetPendingCodesIds(reservation.Id);
                List<int> deletedCodesIds = _codeTransactionService.GetDeletedCodesIds(reservation.Id);

                switch (buildingLockSystem)
                {
                    case BuildingLockSystem.Arches:
                        await HandleArchesUpdate(userName, buildingLockSystem, oldReservation, reservation, oldBuildingUnitId, oldFromDate, oldToDate, newFromDate, newToDate, newBuilingUnitId, hasActiveCodes, pendingCodesIds, deletedCodesIds);
                        break;
                    case BuildingLockSystem.Latch:
                        await HandleLatchUpdate(userName, buildingLockSystem, oldReservation, reservation, oldBuildingUnitId, oldFromDate, oldToDate, newFromDate, newToDate, newBuilingUnitId, hasActiveCodes, pendingCodesIds);
                        break;
                    case BuildingLockSystem.MC:
                        await HandleMCUpdate(reservation, oldReservation);
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }

            async Task HandleLatchUpdate(string userName,
                BuildingLockSystem buildingLockSystem,
                Reservation oldReservation,
                Reservation reservation,
                int oldBuildingUnitId,
                DateTime oldFromDate,
                DateTime oldToDate,
                DateTime newFromDate,
                DateTime newToDate,
                int newBuilingUnitId,
                bool hasActiveCodes,
                List<int> pendingCodesIds)
            {
                bool latchActionCreate = default;
                bool latchActionUpdate = default;
               bool latchActionDelete = default;

                if (!oldReservation.Cancelled && reservation.Cancelled)
                    latchActionDelete = true;
                else if (oldReservation.Cancelled && !reservation.Cancelled)
                    latchActionCreate = true;
                else if (oldReservation.Cancelled && reservation.Cancelled)
                    _loggerService.Add(LogType.Information, $"HandleLatchUpdate - When checking latch action required for update reservation." +
                                                            $" oldReservation and updated reservation were both Cancelled reservations.", reservationDto.Id);
                else if (oldBuildingUnitId != newBuilingUnitId)
                     latchActionUpdate = true;              
                else
                {
                   

                    DateTime currentEstTime = Utilities.GetCurrentTimeInEST();

                    //Handle date changes
                    DateTime reservationStartDate = _dateRepo.GetFromDate(reservation, BuildingLockSystem.Latch, reservation.Id, currentEstTime, isCronJob, true).ToESTDateTime();
                    DateTime oldReservationStartDate = _dateRepo.GetFromDate(oldReservation, BuildingLockSystem.Latch, oldReservation.Id, currentEstTime, isCronJob, true).ToESTDateTime();
                    if (GetEndDate(reservation.EndDate) != GetEndDate(oldReservation.EndDate) || reservationStartDate != oldReservationStartDate)  
                    {
                        //do I need to handle for cases that newEndDate is in the past? The create reservation will fail 
                        latchActionUpdate = true;
                    }

                    //Handle startDate
                    ReservationLatchData reservationLatchData = _reservationLatchDataRepo.Get(x => x.ReservationId == reservation.Id);
                //    var (reservationStarted, reservationStartedAndSetToFuture) = await _latchRepo.CheckIfLatchReservationStartedAndReservationStartChangedToTheFutureAsync(reservation, reservationLatchData, newFromDate, currentEstTime, isCronJob);
                    //_loggerService.Add(LogType.Information, $"HandleLatchUpdate CheckIfLatchReservationStartedAndReservationStartChangedToTheFutureAsync returned..." +
                    //    $" reservationStarted: {reservationStarted}, reservationStartedAndSetToFuture: {reservationStartedAndSetToFuture}.", reservationDto.Id);

                    //if (reservationStartedAndSetToFuture)
                    //{
                    //    latchActionUpdate = true;
                    //}
                    //else if (!reservationStarted)
                    //{
                        //no need to update startdate of already started reservation if the startdate wasn't changed to the future

                       // DateTime reservationStartDate = _dateRepo.GetFromDate(reservation, BuildingLockSystem.Latch, reservation.Id, currentEstTime, isCronJob, true).ToESTDateTime();
                        //DateTime oldReservationStartDate = _dateRepo.GetFromDate(oldReservation, BuildingLockSystem.Latch, oldReservation.Id, currentEstTime, isCronJob, true).ToESTDateTime();
                     
                     //   _loggerService.Add(LogType.Information, $"Comparing start dates. reservationStartDate: {reservationStartDate}. oldReservationStartDate: {oldReservationStartDate}", reservationDto.Id);

                        //if (reservationStartDate != oldReservationStartDate)
                        //    latchActionUpdate = true;
                  //  }

                }

                int numberOfLatchActionsRequired = new List<bool>() { latchActionCreate, latchActionDelete, latchActionUpdate }.Where(x => x != default).Count();

                if (numberOfLatchActionsRequired == 0)
                {
                    _loggerService.Add(LogType.Information, $"HandleLatchUpdate didn't find any latch API calls necessary for this latch reservation update.", reservationDto.Id);
                }

                if (numberOfLatchActionsRequired > 1)
                {
                    throw new Exception($"Found more than one action when attempting to handle a latch reservation update. " +
                        $"actionCreate: {latchActionCreate}. actionDelete: {latchActionDelete}. actionUpdate: {latchActionUpdate}.");
                }

                if (latchActionDelete)
                {
                    _loggerService.Add(LogType.Information, $"HandleLatchUpdate to update this latch reservation necessary to send to latch API. Action of: latchActionDelete.", reservationDto.Id);
                    _codeTransactionService.CreateDeleteCodeTransactionsForInActive(reservation, userName, buildingLockSystem);
                    _codeTransactionService.ExecuteTransactions();
                }
                else if (latchActionCreate)
                {
                    _loggerService.Add(LogType.Information, $"HandleLatchUpdate to update this latch reservation necessary to send to latch API. Action of: latchActionCreate.", reservationDto.Id);
                    await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, buildingLockSystem);
                    int? existingReservationCodeId = _codeTransactionService.GetAllCodesIds(reservation.Id).FirstOrDefault();

                    if (existingReservationCodeId != null)
                        // if there is a there is a reservationCode record connecting to this reservation use that one
                        _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName, (int)existingReservationCodeId);
                    else
                        // otherwise create a new reservationCode record by not passing a ReservationCodeId as an input parameter
                        _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName);

                    _codeTransactionService.ExecuteTransactions();
                }
                else if (latchActionUpdate)
                {
                    _loggerService.Add(LogType.Information, $"HandleLatchUpdate to update this latch reservation necessary to send to latch API. Action of: latchActionUpdate.", reservationDto.Id);
                    _codeTransactionService.CreatePendingCodeTransactionsForChange(reservation, userName, buildingLockSystem, oldReservation.BuildingUnitId);
                    ////create code to delete and to recreate
                    //_codeTransactionService.CreateDeleteCodeTransactionsForInActive(reservation, userName, buildingLockSystem);
                    ////await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, buildingLockSystem);
                    //int? existingReservationCodeId = _codeTransactionService.GetAllCodesIds(reservation.Id).FirstOrDefault();

                    //if (existingReservationCodeId != null)
                    //    // if there is a there is a reservationCode record connecting to this reservation use that one
                    //    _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName, (int)existingReservationCodeId);
                    //else
                    //    // otherwise create a new reservationCode record by not passing a ReservationCodeId as an input parameter
                    //    _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName);

                    _codeTransactionService.ExecuteTransactions();
                }

            }

            async Task HandleArchesUpdate(string userName, BuildingLockSystem buildingLockSystem, Reservation oldReservation, Reservation reservation, int oldBuildingUnitId, DateTime oldFromDate, DateTime oldToDate,
                DateTime newFromDate, DateTime newToDate, int newBuilingUnitId, bool hasActiveCodes, List<int> pendingCodesIds, List<int> deletedCodesIds)
            {
                //bool oldActive = oldReservation.HasValidDates();
                //bool newActive = reservation.HasValidDates();

                bool oldCancelled = oldReservation.Cancelled;
                bool newCancelled = reservation.Cancelled;

                _loggerService.Add(LogType.Information, $"HandleArchesUpdate oldCancelled: {oldCancelled}. newCancelled: {newCancelled}", reservationDto.Id);
                //if (reservation.Cancelled || !reservation.HasValidDates())
                //{
                //    // Delete device lock codes on HomeAssistant api
                //    await DeleteCodesAsync(reservation, true);
                //}
                //else
                //{
                //    // Set device codes on HomeAssistant api, if reservation is active
                //    await SetCodesAsync(reservation, oldReservation);
                // }


                bool? hasAnyInProcessTransaction = _codeTransactionRepo.List(x => x.Status == TransactionStatus.InProcess && x.ReservationId == reservation.Id)?.Any();

                if (pendingCodesIds.HasAny() && hasAnyInProcessTransaction == false)
                {
                    _loggerService.Add(LogType.Information, $"HandleArchesUpdate (pendingCodesIds.HasAny() && hasAnyInProcessTransaction == false) == true", reservationDto.Id);
                    if (!oldCancelled && newCancelled)
                    {
                        //TESTED WORKED!
                        _loggerService.Add(LogType.Information, $"HandleArchesUpdate (!oldCancelled && newCancelled) == true", reservationDto.Id);
                        // create transactions for pending codes, in case of reservation status is changed as "In-Active"
                        await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, buildingLockSystem);
                    }
                    else if (!newBuilingUnitId.Equals(oldBuildingUnitId))
                    {
                        //TESTED WORKED!
                        _loggerService.Add(LogType.Information, $"HandleArchesUpdate (!newBuilingUnitId.Equals(oldBuildingUnitId)) == true", reservationDto.Id);
                        // create transactions for pending codes, in case of reservation "Unit" changed
                        await _codeTransactionService.CreatePendingCodeTransactionsForUnitChangeAsync(reservation, userName, buildingLockSystem);
                    }
                    else
                    {
                        _loggerService.Add(LogType.Information, $"HandleArchesUpdate in the else", reservationDto.Id);
                        if (!newFromDate.Equals(oldFromDate))
                        {
                            //TESTED WORKED!
                            _loggerService.Add(LogType.Information, $"HandleArchesUpdate (!newFromDate.Equals(oldFromDate)) == true", reservationDto.Id);
                            // create transactions for pending codes, in case of reservation "Move-In" date changed
                            await _codeTransactionService.CreatePendingCodeTransactionsForMIChangeAsync(reservation, userName, buildingLockSystem);
                        }

                        if (!newToDate.Equals(oldToDate))
                        {
                            //TESTED WORKED!
                            _loggerService.Add(LogType.Information, $"HandleArchesUpdate (!newToDate.Equals(oldToDate)) == true", reservationDto.Id);
                            // create transactions for pending codes, in case of reservation "Move-Out" date changed
                            await _codeTransactionService.CreatePendingCodeTransactionsForMOChange(reservation, userName, buildingLockSystem);
                        }

                        if (oldCancelled && !newCancelled)
                        {
                            //TESTED WORKED!
                            _loggerService.Add(LogType.Information, $"HandleArchesUpdate (oldCancelled && !newCancelled) == true", reservationDto.Id);
                            // create transactions for pending codes, in case of reservation status is changed as "Active"
                            _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName);
                        }
                    }
                }
                else if (hasActiveCodes || deletedCodesIds.HasAny() || hasAnyInProcessTransaction == true)
                {

                    _loggerService.Add(LogType.Information, $"HandleArchesUpdate in the else if (hasActiveCodes || deletedCodesIds.HasAny() || hasAnyInProcessTransaction == true) == true"
                        , reservationDto.Id);

                    bool hasChangedToInActive = !oldCancelled && newCancelled;
                    bool hasMiOrMoChanged = !newFromDate.Equals(oldFromDate) || !newToDate.Equals(oldToDate);
                    bool hasMiChanged = !newFromDate.Equals(oldFromDate);
                    bool hasMoChanged = !newToDate.Equals(oldToDate);

                    _loggerService.Add(LogType.Information, $"HandleArchesUpdate hasMiMoChanged: {hasMiOrMoChanged}. " +
                        $" newFromDate: {newFromDate}." +
                        $" oldFromDate: {oldFromDate}." +
                        $" newToDate: {newToDate}." +
                        $" oldToDate: {oldToDate}." +
                        $" !newFromDate.Equals(oldFromDate) == {!newFromDate.Equals(oldFromDate)}." +
                        $" !newToDate.Equals(oldToDate) == {!newToDate.Equals(oldToDate)}.", reservationDto.Id);

                    bool hasChangedToActive = oldCancelled && !newCancelled;
                    bool hasUnitChanged = !newBuilingUnitId.Equals(oldBuildingUnitId);

                    if (hasChangedToInActive)
                    {
                        _loggerService.Add(LogType.Information, $"HandleArchesUpdate in the if (hasChangedToInActive) == true", reservationDto.Id);
                        // create transactions for active codes, in case of reservation status is changed as "In-Active"

                        //TESTED WORKED!
                        _codeTransactionService.CreateDeleteCodeTransactionsForInActive(reservation, userName, buildingLockSystem, oldReservation.BuildingUnitId);
                        //problem with the original when not passing in the oldReservation.BuildingUnitId.
                        //Didn't delete the old unit code when posting a update buildingUnit to new buildingUnit in the same post.
                        //Then when runs the codeTransactions tries to delete the new unit, and the old unit really has the code.
                        //should now be fixed
                    }
                    else if (hasUnitChanged && !hasMiOrMoChanged && !hasChangedToActive)
                    {
                        //TESTED WORKED!
                        _loggerService.Add(LogType.Information, $"HandleArchesUpdate (hasUnitChanged && !hasMiMoChanged && !hasChangedToActive) == true", reservationDto.Id);
                        // create transactions for active codes, in case of reservation "Unit" changed
                        _codeTransactionService.CreateActiveCodeTransactionsForUnitChange(reservation, userName, oldBuildingUnitId, buildingLockSystem);
                    }
                    else if (hasChangedToActive)
                    {
                        _loggerService.Add(LogType.Information, $"HandleArchesUpdate in else if hasChangedToActive", reservationDto.Id);
                        if (deletedCodesIds.HasAny())
                        {
                            //TESTED WORKED!
                            _loggerService.Add(LogType.Information, $"HandleArchesUpdate (deletedCodesIds.HasAny()) == true", reservationDto.Id);
                            // update deleted or deleted failed codes status to pending
                            await _reservationCodeRepo.UpdateCodesStatusToPendingAsync(deletedCodesIds);
                        }

                        //TESTED WORKED!
                        // create transactions for active codes, in case of reservation status is changed as "Active"
                        await _codeTransactionService.CreatePendingCodeTransactionsForUnitChangeAsync(reservation, userName, buildingLockSystem);

                        if (hasUnitChanged)
                        {
                            //TESTED WORKED!
                            _loggerService.Add(LogType.Information, $"HandleArchesUpdate hasChangedToActive hasUnitChanged == true", reservationDto.Id);
                            // create old unit delete transaction
                            _codeTransactionService.CreateOldUnitDeleteTransaction(reservation, userName, oldBuildingUnitId, buildingLockSystem);
                        }
                    }
                    else if (hasMiOrMoChanged)
                    {
                        _loggerService.Add(LogType.Information, $"HandleArchesUpdate in else if (hasMiMoChanged) == true", reservationDto.Id);

                        //TESTED WORKED MI MO and both together!
                        // create transactions for active codes, in case of reservation Move-In/Move-Out date changed
                        bool reservationMIDateInTheFuture = DateTime.Compare(
                                Utilities.GetCurrentTimeInEST(),
                                GetStartDate(reservation.StartDate, buildingLockSystem, Utilities.GetCurrentTimeInEST(), isCronJob)
                              ) < 0;

                        //TESTED WORKED MI MO and both together!
                        _codeTransactionService.CreateActiveCodeTransactionsForMIMOChange(reservation, userName, buildingLockSystem, reservationMIDateInTheFuture, hasMoChanged, hasMiChanged);

                        if (hasUnitChanged)
                        {
                            //TESTED WORKED MI MO (I think it works together)!
                            _loggerService.Add(LogType.Information, $"HandleArchesUpdate in hasMiMoChanged (hasUnitChanged) == true", reservationDto.Id);
                            // create old unit delete transaction
                            _codeTransactionService.CreateOldUnitDeleteTransaction(reservation, userName, oldBuildingUnitId, buildingLockSystem);
                        }
                    }
                }

                if (pendingCodesIds.HasAny() || hasActiveCodes || deletedCodesIds.HasAny())
                {
                    _loggerService.Add(LogType.Information, $"HandleArchesUpdate (pendingCodesIds.HasAny() || hasActiveCodes || deletedCodesIds.HasAny()) == true", reservationDto.Id);
                    // start executing pending transactions in background
                    _codeTransactionService.ExecuteTransactions();
                }
            }

            DateTime GetEndDate(DateTime endDate)
            {
                return DateTimeExtension.GetDateTime(endDate, _configuration["ReservationEndTime"]);
            }

            DateTime GetStartDate(DateTime startDate, BuildingLockSystem buildingLockSystem, DateTime currentEstTime, bool isCronJob)
            {
                return DateTimeExtension.GetDateTime(startDate, _dateRepo.GetReservationStartTimeSetting(buildingLockSystem, reservationDto.Id, currentEstTime, isCronJob));
            }
        }

        private async Task HandleMCUpdate( Reservation reservation, Reservation oldReservation)
        {
            //ReservationLatchData reservationLatchData = _stayAccessDbContext.ReservationLatchData.Where(x => x.ReservationId == reservation.Id).FirstOrDefault();
           ReservationMCData reservationMcData = GetFirstOrDefault(reservation.Id);//_stayAccessDbContext.ReservationMCData.FirstOrDefault(x => x.ReservationId == reservation.Id);
            MCReservationUpdateRequest request = new MCReservationUpdateRequest()
            {
                firstname = reservation.FirstName,
                lastname = reservation.LastName,
                check_in = reservation.StartDate.ToString("yyyy-MM-dd"),
                check_out = reservation.EndDate.ToString("yyyy-MM-dd"),
                room_name = reservation.BuildingUnit.UnitId
            };
            _mcService.UpdateReservationAsync( request, reservationMcData, reservation.ReservationMCData.McId, oldReservation).GetAwaiter().GetResult();
        }

        private void ErrorIfCodeContainsBackup(string code, int reservationId)
        {
            if (code.ToLower().Contains("backup"))
            {
                _loggerService.Add(LogType.Information, $"Reservation code looks to similar to backup reservations. It cannot be saved. code: {code}.", reservationId, string.Empty, false);
                throw new Exception($"Reservation code looks to similar to backup reservations. It cannot be saved. code: {code}");
            };
        }

        //public async Task UpdateAsync(ReservationRequestDto reservationDto, string userName, BuildingLockSystem newUnitBuildingLockSystem)
        //{
        //    try
        //    {
        //        Reservation reservation = await GetByIdAsync(reservationDto.Id);

        //        int oldBuildingUnitId = reservation.BuildingUnitId;
        //        int buildingUnitId = reservationDto.GetBuildingUnitId();

        //        int newBuildingUnitBuildingId = _buildingUnitRepo.GetAsync(x => x.Id == buildingUnitId, x => x.Building).Result.BuildingId;
        //        int oldBuildingUnitBuildingId = _buildingUnitRepo.GetAsync(x => x.Id == oldBuildingUnitId, x => x.Building).Result.BuildingId;

        //        if (oldBuildingUnitBuildingId != newBuildingUnitBuildingId)
        //            throw new Exception("Can't change reservation.buildingUnitId to buildingUnitId of a unit in a different building");

        //        DateTime oldFromDate = reservation.FromDate();
        //        DateTime oldToDate = reservation.ToDate();
        //        bool oldActive = reservation.HasValidDates();

        //        DateTime startDate = DateTimeExtension.GetDateTime(reservationDto.StartDate, _dateRepo.GetReservationStartTimeSetting(newUnitBuildingLockSystem));
        //        DateTime endDate = DateTimeExtension.GetDateTime(reservationDto.EndDate, _configuration["ReservationEndTime"]);

        //        bool isModified = !reservationDto.BuildingUnitId.Equals(reservationDto.NewBuildingUnitId) || !reservationDto.Code.Equals(reservationDto.NewCode) || !reservation.EndDate.Equals(endDate);

        //        if (reservation is null)
        //            throw new Exception("Reservation not found.");

        //        DAL.DomainEntities.Reservation oldReservation = null;
        //        if (isModified)
        //            oldReservation = reservation.DeepClone();

        //        reservation.Id = reservationDto.Id;
        //        reservation.BuildingUnitId = buildingUnitId;
        //        reservation.Code = reservationDto.GetCode();
        //        reservation.StartDate = startDate;
        //        reservation.EndDate = endDate;
        //        reservation.EarlyCheckIn = reservationDto.EarlyCheckIn;
        //        reservation.LateCheckOut = reservationDto.LateCheckOut;
        //        reservation.Cancelled = reservationDto.Cancelled;
        //        reservation.ModifiedDate = DateTime.UtcNow;
        //        reservation.ModifiedBy = userName;

        //        _loggerService.Add(LogType.Information, $"Updating reservation: {JsonConvert.SerializeObject(reservation)}.", reservation.Id, string.Empty, false);
        //        _reservationRepo.UpdateWithSave(reservation);
        //        _loggerService.Add(LogType.Information, $"Reservation updated successfully for: {JsonConvert.SerializeObject(reservation)}.", reservation.Id);

        //        //if (reservation.Cancelled || !reservation.HasValidDates())
        //        //{
        //        //    // Delete device lock codes on HomeAssistant api
        //        //    await DeleteCodesAsync(reservation, true);
        //        //}
        //        //else
        //        //{
        //        //    // Set device codes on HomeAssistant api, if reservation is active
        //        //    await SetCodesAsync(reservation, oldReservation);
        //        //}

        //        DateTime newFromDate = reservation.FromDate();
        //        DateTime newToDate = reservation.ToDate();
        //        int newBuilingUnitId = reservation.BuildingUnitId;
        //        bool newActive = reservation.HasValidDates();
        //        bool hasActiveCodes = _codeTransactionService.HasActiveCodes(reservation.Id);

        //        List<int> pendingCodesIds = _codeTransactionService.GetPendingCodesIds(reservation.Id);
        //        List<int> deletedCodesIds = _codeTransactionService.GetDeletedCodesIds(reservation.Id);

        //        if (pendingCodesIds.HasAny())
        //        {
        //            if (oldActive && !newActive || !newBuilingUnitId.Equals(oldBuildingUnitId))
        //            {
        //                if (oldActive && !newActive)
        //                {
        //                    // create transactions for pending codes, in case of reservation status is changed as "In-Active"
        //                    await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, newUnitBuildingLockSystem);
        //                }
        //                else if (!newBuilingUnitId.Equals(oldBuildingUnitId) && newUnitBuildingLockSystem == BuildingLockSystem.Arches)
        //                {
        //                    switch (newUnitBuildingLockSystem)
        //                    {
        //                        case BuildingLockSystem.Arches:
        //                            // create transactions for pending codes, in case of reservation "Unit" changed
        //                            await _codeTransactionService.CreatePendingCodeTransactionsForUnitChangeAsync(reservation, userName, newUnitBuildingLockSystem);
        //                            break;
        //                        case BuildingLockSystem.Latch:
        //                            _codeTransactionService.CreatePendingCodeTransactionsForChange(reservation, userName, newUnitBuildingLockSystem);
        //                            break;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                switch (newUnitBuildingLockSystem)
        //                {
        //                    case BuildingLockSystem.Arches:
        //                        if (!newFromDate.Equals(oldFromDate))

        //                            // create transactions for pending codes, in case of reservation "Move-In" date changed
        //                            await _codeTransactionService.CreatePendingCodeTransactionsForMIChangeAsync(reservation, userName, newUnitBuildingLockSystem);

        //                        if (!newToDate.Equals(oldToDate))

        //                            // create transactions for pending codes, in case of reservation "Move-Out" date changed
        //                            _codeTransactionService.CreatePendingCodeTransactionsForMOChange(reservation, userName, newUnitBuildingLockSystem);

        //                        if (!oldActive && newActive)

        //                            // create transactions for pending codes, in case of reservation status is changed as "Active"
        //                            _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName);
        //                        break;

        //                    case BuildingLockSystem.Latch:
        //                        if (isModified)//(!newFromDate.Equals(oldFromDate) || !newToDate.Equals(oldToDate) || !oldActive && newActive)
        //                            _codeTransactionService.CreatePendingCodeTransactionsForChange(reservation, userName, newUnitBuildingLockSystem);
        //                        break;
        //                }
        //            }
        //        }
        //        else if (hasActiveCodes || deletedCodesIds.HasAny())
        //        {
        //            bool hasChangedToInActive = oldActive && !newActive;
        //            bool hasMiMoChanged = !newFromDate.Equals(oldFromDate) || !newToDate.Equals(oldToDate);
        //            bool hasChangedToActive = !oldActive && newActive;
        //            bool hasUnitChanged = !newBuilingUnitId.Equals(oldBuildingUnitId);

        //            if (hasChangedToInActive)
        //            {
        //                // create transactions for active codes, in case of reservation status is changed as "In-Active"
        //                _codeTransactionService.CreateActiveCodeTransactionsForInActive(reservation, userName, newUnitBuildingLockSystem);
        //            }
        //            else
        //            {
        //                switch (newUnitBuildingLockSystem)
        //                {
        //                    case BuildingLockSystem.Arches:
        //                        if (hasUnitChanged && !hasMiMoChanged && !hasChangedToActive)
        //                        {
        //                            // create transactions for active codes, in case of reservation "Unit" changed
        //                            _codeTransactionService.CreateActiveCodeTransactionsForUnitChange(reservation, userName, oldBuildingUnitId, newUnitBuildingLockSystem);
        //                        }
        //                        else if (hasChangedToActive)
        //                        {
        //                            if (deletedCodesIds.HasAny())
        //                            {
        //                                // update deleted or deleted failed codes status to pending
        //                                await _reservationCodeRepo.UpdateCodesStatusToPendingAsync(deletedCodesIds);
        //                            }
        //                            // create transactions for active codes, in case of reservation status is changed as "Active"
        //                            await _codeTransactionService.CreatePendingCodeTransactionsForUnitChangeAsync(reservation, userName, newUnitBuildingLockSystem);
        //                            if (hasUnitChanged)
        //                                // create old unit delete transaction
        //                                _codeTransactionService.CreateOldUnitDeleteTransaction(reservation, userName, oldBuildingUnitId, newUnitBuildingLockSystem);
        //                        }
        //                        else if (hasMiMoChanged)
        //                        {
        //                            // create transactions for active codes, in case of reservation Move-In/Move-Out date changed
        //                            _codeTransactionService.CreateActiveCodeTransactionsForMIMOChange(reservation, userName, newUnitBuildingLockSystem);
        //                            if (hasUnitChanged)
        //                                // create old unit delete transaction
        //                                _codeTransactionService.CreateOldUnitDeleteTransaction(reservation, userName, oldBuildingUnitId, newUnitBuildingLockSystem);
        //                        }
        //                        break;
        //                    case BuildingLockSystem.Latch:
        //                        if (hasChangedToActive && deletedCodesIds.HasAny())
        //                        {
        //                            // update deleted or deleted failed codes status to pending
        //                            await _reservationCodeRepo.UpdateCodesStatusToPendingAsync(deletedCodesIds);
        //                        }
        //                        //if ((hasUnitChanged && !hasMiMoChanged && !hasChangedToActive) || hasChangedToActive || hasMiMoChanged)

        //                        if (hasChangedToActive)
        //                        {
        //                            _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName);
        //                        }
        //                        else if (hasChangedToInActive)
        //                        {
        //                            await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, newUnitBuildingLockSystem);
        //                            _codeTransactionService.CreateActiveCodeTransactionsForInActive(reservation, userName, newUnitBuildingLockSystem);
        //                        }
        //                        //else if (_latchRepo.LatchReservationStartedAndReservationStartChangedToTheFuture(reservation, startDate, Utilities.GetCurrentTimeInEST()).reservationStartedAndSetToFuture)
        //                        //{
        //                        //    _codeTransactionService.CreateActiveCodeTransactionsForInActive(reservation, userName, newUnitBuildingLockSystem);
        //                        //    _codeTransactionService.CreatePendingCodeTransactionsForNew(reservation, userName);
        //                        //}
        //                        else if (isModified)
        //                        {
        //                            _codeTransactionService.CreatePendingCodeTransactionsForChange(reservation, userName, newUnitBuildingLockSystem);
        //                        }
        //                        break;
        //                }
        //            }
        //        }

        //        if (pendingCodesIds.HasAny() || hasActiveCodes || deletedCodesIds.HasAny())
        //        // || (reservationBuildingLockSystem == BuildingLockSystem.Latch && DateTime.Compare(newFromDate, DateTime.Now.AddMinutes(45)) < 0))
        //        {
        //            // start executing pending transactions in background
        //            _codeTransactionService.ExecuteTransactions();
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public async Task EarlyCheckInAsync(int reservationId, string userName)
        {
            try
            {
                DAL.DomainEntities.Reservation reservation = await GetByIdAsync(reservationId);

                if (reservation is null)
                    throw new Exception("Invalid id reservation not found.");

                reservation.EarlyCheckIn = DateTime.Now;
                reservation.ModifiedDate = DateTime.UtcNow;
                reservation.ModifiedBy = userName;

                bool isArchesBackupReservation = _usedBackupCodesService.IsArchesBackupReservation(reservationId).Result.isValid;

                if (isArchesBackupReservation)
                {
                    reservation.Code = _usedBackupCodesService.ToCodeAsUsed(reservation.Code);
                }


                _loggerService.Add(LogType.Information, $"Updating reservation for early checkin: {JsonConvert.SerializeObject(reservation)}.", reservationId, string.Empty, false);
                _reservationRepo.UpdateWithSave(reservation);
                _loggerService.Add(LogType.Information, $"Reservation updated successfully for early checkin: {JsonConvert.SerializeObject(reservation)}.", reservationId);


                if (isArchesBackupReservation)
                {
                    await _usedBackupCodesService.NotifyBackupMarkedAsUsedAsync(reservation.Id, userName);
                }

                BuildingLockSystem reservationBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystem(reservation);
                switch (reservationBuildingLockSystem)
                {
                    case BuildingLockSystem.Arches:
                        // create transactions for pending codes, in case of reservation "Move-In" date changed
                        await _codeTransactionService.CreatePendingCodeTransactionsForMIChangeAsync(reservation, userName, reservationBuildingLockSystem);

                        // create transactions for active codes, in case of reservation Move-In/Move-Out date changed
                        _codeTransactionService.CreateActiveCodeTransactionsForMIMOChange(reservation, userName, reservationBuildingLockSystem);
                        // Set device codes on HomeAssistant api, if reservation is active
                        //await SetCodesAsync(reservation, null);
                        break;
                    case BuildingLockSystem.Latch:
                        _codeTransactionService.CreatePendingCodeTransactionsForChange(reservation, userName, reservationBuildingLockSystem, null);
                        break;
                }

                // start executing pending transactions in background
                _codeTransactionService.ExecuteTransactions();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task LateCheckOutAsync(int reservationId, string userName)
        {
            try
            {
                DAL.DomainEntities.Reservation reservation = await GetByIdAsync(reservationId);

                if (reservation is null)
                    throw new Exception("Invalid id reservation not found.");

                reservation.LateCheckOut = DateTime.Now;
                reservation.ModifiedDate = DateTime.UtcNow;
                reservation.ModifiedBy = userName;

                bool isArchesBackupReservation = _usedBackupCodesService.IsArchesBackupReservation(reservation.Id).Result.isValid;
                if (isArchesBackupReservation)
                {
                    reservation.Code = _usedBackupCodesService.ToCodeAsUsed(reservation.Code);
                }

                _loggerService.Add(LogType.Information, $"Updating reservation for late checkout: {JsonConvert.SerializeObject(reservation)}.", reservationId, string.Empty, false);
                _reservationRepo.UpdateWithSave(reservation);
                _loggerService.Add(LogType.Information, $"Reservation updated successfully for late checkout: {JsonConvert.SerializeObject(reservation)}.", reservationId);

                if (isArchesBackupReservation)
                {
                    await _usedBackupCodesService.NotifyBackupMarkedAsUsedAsync(reservation.Id, userName);
                }

                BuildingLockSystem reservationBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystem(reservation);
                switch (reservationBuildingLockSystem)
                {
                    case BuildingLockSystem.Arches:
                        // create transactions for pending codes, in case of reservation "Move-Out" date changed
                        await _codeTransactionService.CreatePendingCodeTransactionsForMOChange(reservation, userName, reservationBuildingLockSystem);

                        // create transactions for active codes, in case of reservation Move-In/Move-Out date changed
                        _codeTransactionService.CreateActiveCodeTransactionsForMIMOChange(reservation, userName, reservationBuildingLockSystem);
                        // Delete device lock codes on HomeAssistant api
                        //await DeleteCodesAsync(reservation, true);
                        break;
                    case BuildingLockSystem.Latch:
                        _codeTransactionService.CreatePendingCodeTransactionsForChange(reservation, userName, reservationBuildingLockSystem, null);
                        break;
                }

                // start executing pending transactions in background
                _codeTransactionService.ExecuteTransactions();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int reservationId, string userName)
        {
            try
            {
                Reservation reservation = await GetByIdAsync(reservationId);

                if (reservation is null)
                    throw new Exception("Reservation not found.");

                // _loggerRepo.Add(LogType.Information, $"Deleting Device Codes for reservation Id: {reservationId}.", reservationId);
                // Delete device lock codes on HomeAssistant api
                //await DeleteCodesAsync(reservation, true);
                BuildingLockSystem reservationBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystem(reservation);
                IQueryable<CodeTransaction> codeTransactions = _codeTransactionRepo.List(x => x.ReservationId == reservation.Id);

                switch (reservationBuildingLockSystem)
                {
                    case BuildingLockSystem.Arches:
                        // create transactions for pending codes, in case of reservation status is changed as "In-Active"
                        await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, reservationBuildingLockSystem);

                        // create transactions for active codes, in case of reservation status is changed as "In-Active"
                        _codeTransactionService.CreateDeleteCodeTransactionsForInActive(reservation, userName, reservationBuildingLockSystem);
                        break;
                    case BuildingLockSystem.Latch:
                        //Rule for latch - shouldn't allow to delete if any of the code transaction records were executed.
                        if (codeTransactions.Where(x => x.Status == TransactionStatus.Executed).Any())//ALWAYS TRUE!
                        {
                            _loggerService.Add(LogType.Error, $"Error occurred when attempting to delete reservation. Error: Latch reservation has CodeTransaction record(s) that have a status of executed. Reservation: {JsonConvert.SerializeObject(reservation)}.", reservationId);
                            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized) { ReasonPhrase = "Latch reservation has CodeTransaction record(s) that have a status of executed" });

                        }
                        else
                        {
                            _loggerService.Add(LogType.Information, $"Setting create code transactions of status pending to status deleted. Reservation: {JsonConvert.SerializeObject(reservation)}.", reservationId);
                            await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, reservationBuildingLockSystem);
                        }
                        break;
                      
                }

                (bool isValid, Reservation reservation) isArchesBackupReservation = await _usedBackupCodesService.IsArchesBackupReservation(reservationId);

                if (isArchesBackupReservation.isValid)
                {
                    ReservationRequestDto reservationRequestDto = _usedBackupCodesService.GetBackupReservationDtoToSetAsUsed(isArchesBackupReservation.reservation);
                    await UpdateAsync(reservationRequestDto, userName, reservationBuildingLockSystem);
                }

                // start executing pending transactions
                _codeTransactionService.ExecuteTransactions(true);

                //logs call save changes -- put all logs before the data changes -- call save changes once, either all succeed or all fail
                _loggerService.Add(LogType.Information, $"Deleting from the database reservation and it's reservation codes. Reservation: {JsonConvert.SerializeObject(reservation)}.", reservationId);
                _loggerService.Add(LogType.Information, $"Setting Logger.ReservationId to null. For logs of ReservationId: {reservation.Id}. Reservation: {JsonConvert.SerializeObject(reservation)}.", reservationId);

                if (reservationBuildingLockSystem == BuildingLockSystem.Latch)
                {
                    _loggerService.Add(LogType.Information, $"Deleting any reservation CodeTransactions of ReservationId: {reservationId} when attempting to delete reservation. Reservation: {JsonConvert.SerializeObject(reservation)}.", reservationId);

                    //no logger.Add after this until save (logger saves to database)
                    _codeTransactionRepo.DeleteRange(codeTransactions);
                }

                // delete all reservation codes if there are any
                _reservationCodeRepo.DeleteWithoutSave(x => x.ReservationId == reservation.Id);

                // set reservationId to null for reservation's logs
                List<Logger> logs = await _loggerRepo.List(x => x.ReservationId == reservation.Id).ToListAsync();
                if (logs.HasAny())
                {
                    foreach (var log in logs)
                    {
                        log.ReservationId = null;
                    }
                }

                // delete reservation
                _reservationRepo.DeleteWithSave(reservation);

                _loggerService.Add(LogType.Information, $"Reservation deleted successfully. Reservation: {JsonConvert.SerializeObject(reservation)}.", null);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in deleting Reservation. Error: {ex}", reservationId);
                throw;
            }
        }

        public async Task CancelledAsync(int reservationId, string userName)
        {
            try
            {
                DAL.DomainEntities.Reservation reservation = await GetByIdAsync(reservationId);

                if (reservation is null)
                    throw new Exception("Reservation not found.");

                bool isArchesBackupReservation = _usedBackupCodesService.IsArchesBackupReservation(reservation.Id).Result.isValid;
                if (isArchesBackupReservation)
                {
                    reservation.Code = _usedBackupCodesService.ToCodeAsUsed(reservation.Code);
                }

                reservation.Cancelled = true;
                reservation.ModifiedDate = DateTime.UtcNow;
                reservation.ModifiedBy = userName;

                _loggerService.Add(LogType.Information, $"Updating reservation for cancellation (database update): {JsonConvert.SerializeObject(reservation)}.", reservationId, string.Empty, false);
                _reservationRepo.UpdateWithSave(reservation);
                _loggerService.Add(LogType.Information, $"Reservation updated successfully for cancellation (database update): {JsonConvert.SerializeObject(reservation)}.", reservationId);

                if (isArchesBackupReservation)
                {
                    await _usedBackupCodesService.NotifyBackupMarkedAsUsedAsync(reservation.Id, userName);
                }

                BuildingLockSystem reservationBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystem(reservation);

                // create transactions for pending codes, in case of reservation status is changed as "In-Active"
                await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, userName, reservationBuildingLockSystem);

                // create transactions for active codes, in case of reservation status is changed as "In-Active"
                _codeTransactionService.CreateDeleteCodeTransactionsForInActive(reservation, userName, reservationBuildingLockSystem);

                // Delete device lock codes on HomeAssistant api
                //await DeleteCodesAsync(reservation, true);

                // start executing pending transactions in background
                _codeTransactionService.ExecuteTransactions();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Reservation> GetByIdAsync(int reservationId)
        {
            try
            {
                return await _reservationRepo.GetAsync(x => x.Id == reservationId, a => a.BuildingUnit);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<DAL.DomainEntities.Reservation> GetAsync(Expression<Func<DAL.DomainEntities.Reservation, bool>> predicate, params Expression<Func<DAL.DomainEntities.Reservation, object>>[] includes)
        {
            try
            {
                return await _reservationRepo.GetAsync(predicate, includes);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<DAL.DomainEntities.Reservation>> ListAsync(Expression<Func<DAL.DomainEntities.Reservation, bool>> predicate)
        {
            try
            {
                return await _reservationRepo.List(predicate).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataSourceResultDto PagedList(FetchRequestDto request)
        {
            try
            {
                IQueryable<Reservation> query = _stayAccessDbContext.Reservation.Include(x => x.BuildingUnit.Building).Include(x => x.ReservationCodes).Include(x => x.ReservationLatchData);

                //IQueryable<Reservation> query;

                //IQueryable<Reservation> query = GetIQueryableReservationsWithCode();

                //IIncludableQueryable.


                //    return _reservationRepo.List(predicate, a => a.BuildingUnit.Building, a => a.ReservationCodes);

                if (request.Filters.HasAny())
                {
                    foreach (FilterDto filter in request.Filters)
                    {
                        if (!string.IsNullOrEmpty(filter.Field))
                        {
                            string field = filter.Field.ToLower();
                            string value = !string.IsNullOrEmpty(filter.Value) ? filter.Value.ToLower() : string.Empty;

                            if (field.Equals("id"))
                                query = query.Where(x => x.Id.ToString().Equals(value));

                            if (field.Equals("buildingunitunitid"))
                                query = query.Where(x => x.BuildingUnit.UnitId.Equals(value));

                            if (field.Equals("buildingunitid"))
                                query = query.Where(x => x.BuildingUnit.Id.ToString().Equals(value));

                            if (field.Equals("buildingid"))
                                query = query.Where(x => x.BuildingUnit.BuildingId.ToString().Equals(value));

                            if (field.Equals("buildingname"))
                                query = query.Where(x => x.BuildingUnit.Building.Name.Equals(value));

                            if (field.Equals("startdate"))
                                query = query.Where(x => x.StartDate.Date == Convert.ToDateTime(value).Date);

                            if (field.Equals("enddate"))
                                query = query.Where(x => x.EndDate.Date == Convert.ToDateTime(value).Date);

                            if (field.Equals("earlycheckin"))
                                query = query.Where(x => x.EarlyCheckIn.HasValue && x.EarlyCheckIn.Value.Date == Convert.ToDateTime(value).Date);

                            if (field.Equals("latecheckout"))
                                query = query.Where(x => x.LateCheckOut.HasValue && x.LateCheckOut.Value.Date == Convert.ToDateTime(value).Date);

                            if (field.Equals("code"))
                                query = query.Where(x => x.Code.ToLower().Equals(value));

                            //if (field.Equals("latchreservationtoken"))
                            //    query = query.Where(x => x.ReservationLatchData.LatchReservationToken.ToLower().Equals(value));

                            //reservation's reservationCode
                            if (field.Equals("reservationcodeid"))
                                query = query.Where(x => x.ReservationCodes.FirstOrDefault().Id.ToString().Equals(value));

                            if (field.Equals("lockcode"))
                                query = query.Where(x => x.ReservationCodes.FirstOrDefault().LockCode.ToLower().Equals(value));

                            if (field.Equals("slotno"))
                                query = query.Where(x => x.ReservationCodes.FirstOrDefault().SlotNo.ToString().Equals(value));

                            if (field.Equals("status"))
                                query = query.Where(x => x.ReservationCodes.FirstOrDefault().Status.Equals(Enum.Parse(typeof(CodeStatus), value)));

                        }
                    }
                }

                if (request.Sorts.HasAny())
                {
                    SortDto sort = request.Sorts.FirstOrDefault();
                    if (!string.IsNullOrEmpty(sort?.Field))
                    {
                        bool isAscending = sort.Direction.Equals("asc");
                        switch (sort?.Field.ToLower())
                        {
                            case "id":
                                query = isAscending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
                                break;

                            case "buildingunitunitid":
                                query = isAscending ? query.OrderBy(x => x.BuildingUnit.UnitId) : query.OrderByDescending(x => x.BuildingUnit.UnitId);
                                break;

                            case "buildingunitid":
                                query = isAscending ? query.OrderBy(x => x.BuildingUnit.Id) : query.OrderByDescending(x => x.BuildingUnit.Id);
                                break;

                            case "buildingid":
                                query = isAscending ? query.OrderBy(x => x.BuildingUnit.BuildingId) : query.OrderByDescending(x => x.BuildingUnit.BuildingId);
                                break;

                            case "buildingname":
                                query = isAscending ? query.OrderBy(x => x.BuildingUnit.Building.Name) : query.OrderByDescending(x => x.BuildingUnit.Building.Name);
                                break;

                            case "startdate":
                                query = isAscending ? query.OrderBy(x => x.StartDate) : query.OrderByDescending(x => x.StartDate);
                                break;
                            case "enddate":
                                query = isAscending ? query.OrderBy(x => x.EndDate) : query.OrderByDescending(x => x.EndDate);
                                break;
                            case "earlycheckin":
                                query = isAscending ? query.OrderBy(x => x.EarlyCheckIn) : query.OrderByDescending(x => x.EarlyCheckIn);
                                break;
                            case "latecheckout":
                                query = isAscending ? query.OrderBy(x => x.LateCheckOut) : query.OrderByDescending(x => x.LateCheckOut);
                                break;
                            case "code":
                                query = isAscending ? query.OrderBy(x => x.Code) : query.OrderByDescending(x => x.Code);
                                break;

                            //case "latchreservationtoken":
                            //    query = isAscending ? query.OrderBy(x => x.ReservationLatchData.LatchReservationToken) : query.OrderByDescending(x => x.ReservationLatchData.LatchReservationToken);
                            //    break;

                            //reservation's reservationCode
                            case "reservationcodeid":
                                query = isAscending ? query.OrderBy(x => x.ReservationCodes.FirstOrDefault().Id) : query.OrderByDescending(x => x.ReservationCodes.FirstOrDefault().Id);
                                break;
                            case "lockcode":
                                query = isAscending ? query.OrderBy(x => x.ReservationCodes.FirstOrDefault().LockCode) : query.OrderByDescending(x => x.ReservationCodes.FirstOrDefault().LockCode);
                                break;
                            case "slotno":
                                query = isAscending ? query.OrderBy(x => x.ReservationCodes.FirstOrDefault().SlotNo) : query.OrderByDescending(x => x.ReservationCodes.FirstOrDefault().SlotNo);
                                break;
                            case "status":
                                query = isAscending ? query.OrderBy(x => x.ReservationCodes.FirstOrDefault().Status) : query.OrderByDescending(x => query.OrderBy(x => x.ReservationCodes.FirstOrDefault().Status));
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id);
                }

                List<ReservationResponseDto> reservationDtos = new();
                DataSourceResultDto dataSourceResult = _reservationRepo.PagedList(query, request.Page, request.PageSize);
                var reservations = (List<DAL.DomainEntities.Reservation>)dataSourceResult.Data;
                if (reservations.HasAny())
                {
                    foreach (var reservation in reservations)
                    {
                        ReservationCode reservationCode = reservation.ReservationCodes.FirstOrDefault();

                        ReservationResponseDto reservationResponseDto = new()
                        {
                            Id = reservation.Id,
                            BuildingUnitId = reservation.BuildingUnitId,
                            Code = reservation.Code,
                            StartDate = reservation.StartDate,
                            EndDate = reservation.EndDate,
                            EarlyCheckIn = reservation.EarlyCheckIn,
                            LateCheckOut = reservation.LateCheckOut,
                            Cancelled = reservation.Cancelled,
                            Active = reservation.IsCurrentActiveReservation(),
                            BuildingUnitUnitId = reservation.BuildingUnit.UnitId,
                            BuildingId = reservation.BuildingUnit.BuildingId,
                            BuildingName = reservation.BuildingUnit.Building.Name,
                        };

                        if (reservationCode != default)
                            reservationResponseDto.ReservationReservationCodeResponse = new()
                            {
                                ReservationCodeId = reservationCode.Id,
                                LockCode = reservationCode.LockCode,
                                SlotNo = reservationCode.SlotNo,
                                Status = reservationCode.Status,
                            };

                        reservationDtos.Add(reservationResponseDto);
                    }
                }

                dataSourceResult.Data = reservationDtos;
                return dataSourceResult;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IQueryable<DAL.DomainEntities.Reservation> GetIQueryableReservations(Expression<Func<DAL.DomainEntities.Reservation, bool>> predicate = null)
        {
            try
            {
                return _reservationRepo.List(predicate, a => a.BuildingUnit);
            }
            catch (Exception)
            {
                throw;
            }
        }

        //public IQueryable<DAL.DomainEntities.Reservation> GetIQueryableReservationsWithCode(Expression<Func<DAL.DomainEntities.Reservation, bool>> predicate = null)
        //{
        //    try
        //    {
        //        return _reservationRepo.List(predicate, a => a.BuildingUnit.Building, a => a.ReservationCodes);
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public async Task<Reservation> GetMatchedReservationAsync(ReservationRequestDto reservationDto, BuildingLockSystem newUnitBuildingLockSystem, bool isCronJob = false)
        {
            try
            {
                int buildingUnitId = reservationDto.GetBuildingUnitId();
                string code = reservationDto.GetCode();
                DateTime startDate = DateTimeExtension.GetDateTime(reservationDto.StartDate, _dateRepo.GetReservationStartTimeSetting(newUnitBuildingLockSystem, reservationDto.Id, Utilities.GetCurrentTimeInEST(), isCronJob));
                DateTime endDate = DateTimeExtension.GetDateTime(reservationDto.EndDate, _configuration["ReservationEndTime"]);
                return await GetAsync(x => x.BuildingUnitId == buildingUnitId && x.Code.Equals(code) &&
                                           x.StartDate.Equals(startDate) && x.EndDate.Equals(endDate) &&
                                           x.EarlyCheckIn.Equals(reservationDto.EarlyCheckIn) && x.LateCheckOut.Equals(reservationDto.LateCheckOut) &&
                                           x.Cancelled == reservationDto.Cancelled &&
                                           x.FirstName == reservationDto.FirstName &&
                                           x.LastName == reservationDto.LastName);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AdjustReservationCodeDto(ReservationCodeDto reservationCodeDto, BuildingLockSystem reservationBuildingLockSystem)
        {
            try
            {
                if (reservationBuildingLockSystem == BuildingLockSystem.Latch)
                {
                    //set to default, these have no meaning in latch, prevent db duplicates
                    reservationCodeDto.Id = default;
                    reservationCodeDto.ReservationId = default;
                    reservationCodeDto.LockCode = "default";
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<BuildingLockSystem> AdjustReservationAddWithCodesDto(ReservationWithCodesDto reservationDto)
        {
            try
            {
                BuildingLockSystem reservationBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystemByBuildingUnitId(reservationDto.GetBuildingUnitId());
                if (reservationBuildingLockSystem == BuildingLockSystem.Latch || reservationBuildingLockSystem ==BuildingLockSystem.MC)
                {
                    //set to default, these have no meaning in latch, prevent db duplicates
                    reservationDto.Id = default;
                    reservationDto.NewBuildingUnitId = default;
                    reservationDto.NewCode = default;
                    reservationDto.ReservationCodes = new()
                    {
                        new()
                        {
                            Id = default,
                            ReservationId = default,
                            LockCode = "",
                        }
                    };
                }
                return reservationBuildingLockSystem;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<BuildingLockSystem> AdjustReservationRequestDto(ReservationRequestDto reservationDto)
        {
            try
            {
                BuildingLockSystem reservationBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystemByBuildingUnitId(reservationDto.GetBuildingUnitId());
                if (reservationBuildingLockSystem == BuildingLockSystem.Latch || reservationBuildingLockSystem == BuildingLockSystem.MC)
                {
                    //set to default, these have no meaning in latch, prevent db duplicates
                    reservationDto.NewBuildingUnitId = default;
                    reservationDto.NewCode = default;
                }
                return reservationBuildingLockSystem;
            }
            catch
            {
                throw;
            }
        }

        public async Task<ReservationCodeResponseDto> AddWithCodes(ReservationWithCodesDto reservationDto, ReservationCodeResponseDto responseDto, bool isBackupReservation, string userName)
        {
            BuildingLockSystem newUnitBuildingLockSystem = await AdjustReservationAddWithCodesDto(reservationDto);
            var matchedReservation = await GetMatchedReservationAsync(reservationDto, newUnitBuildingLockSystem);
            if (matchedReservation != null)
                responseDto.ReservationId = matchedReservation.Id;
            else
                responseDto = await AddAsync(reservationDto, userName, newUnitBuildingLockSystem, isBackupReservation, reservationDto.ReservationCodes);
            return responseDto;
        }
        public ReservationMCData GetFirstOrDefault(int reservationId)
        {
            return _reservationMCDataRepo.Get(x => x.ReservationId == reservationId);
        }
        public async Task<List<CurrentReservationResponseDto>> GetAllArchesCurrentReservationsAsync()
        {
            DateTime estNow = DateTime.UtcNow.ToESTDateTime();
            List<CurrentReservationResponseDto> currentActiveArchesReservations = await _stayAccessDbContext.Reservation.Include(x => x.BuildingUnit.Building).Include(x => x.ReservationCodes)
                                                                        .Where(x => x.Cancelled == false && x.BuildingUnit.Building.BuildingLockSystem == BuildingLockSystem.Arches
                                                                                    &&
                                                                                    (x.StartDate < estNow || x.EarlyCheckIn < estNow)
                                                                                    &&
                                                                                    (x.EndDate > estNow || x.LateCheckOut > estNow)
                                                                                )
                                                                        .Select(x =>
                                                                            new CurrentReservationResponseDto
                                                                            {
                                                                                ReservationId = x.Id,
                                                                                StartDate = x.StartDate,
                                                                                EndDate = x.EndDate,
                                                                                EarlyCheckIn = x.EarlyCheckIn,
                                                                                LateCheckOut = x.LateCheckOut,
                                                                                FrontDoorUserId = x.FrontDoorUserId,
                                                                                BuildingUnit = x.BuildingUnit.UnitId,
                                                                                Slot = x.ReservationCodes.Select(x => x.SlotNo).FirstOrDefault(),
                                                                                LockCode = x.ReservationCodes.Select(x => x.LockCode).FirstOrDefault(),
                                                                            }
                                                                        ).ToListAsync();
            return currentActiveArchesReservations;
        }

        //private async Task SetCodesAsync(DAL.DomainEntities.Reservation reservation, DAL.DomainEntities.Reservation oldReservation)
        //{
        //    try
        //    {
        //        var reservationCodesList = await _reservationCodeRepo.ListByReservationIdAsync(reservation.Id);



        //        switch (reservation.BuildingUnit.Building.BuildingLockSystem)
        //        {
        //            case BuildingLockSystem.Arches:
        //                // set device lock codes on HomeAssistant api
        //                _homeAssistantRepo.SetCodesForUnit(reservation, reservationCodesList, false, oldReservation);
        //                break;
        //            case BuildingLockSystem.Latch:
        //                _latchRepo.SetCodesForDoor(reservation, reservationCodesList, false, oldReservation);
        //                break;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //private async Task DeleteCodesAsync(DAL.DomainEntities.Reservation reservation, bool deleteFrontDoorCode = false)
        //{
        //    try
        //    {
        //        var reservationCodesList = await _reservationCodeRepo.ListByReservationIdAsync(reservation.Id);


        //        switch (reservation.BuildingUnit.Building.BuildingLockSystem)
        //        {
        //            case BuildingLockSystem.Arches:
        //                // delete device lock codes on HomeAssistant api
        //                _homeAssistantRepo.DeleteCodesForUnit(reservation, reservationCodesList, false, deleteFrontDoorCode);
        //                break;
        //            case BuildingLockSystem.Latch:
        //                _latchRepo.DeleteCodesForLatch(reservation, reservationCodesList, false);
        //                break;
        //        }

        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
    }
}