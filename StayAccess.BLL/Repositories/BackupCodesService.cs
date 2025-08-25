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
using StayAccess.DTO.ReservationCode;
using StayAccess.DTO.Reservations;
using StayAccess.Tools;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace StayAccess.BLL.Repositories
{
    public class BackupCodesService : IBackupCodesService
    {
        private readonly DateTime _endDate = DateTime.UtcNow.Date.AddYears(1);
        private readonly DateTime currentEstDateTime = Utilities.GetCurrentTimeInEST();

        private readonly IConfiguration _configuration;
        private readonly int _amountOfBackupsPerBuildingUnit;

        private readonly StayAccessDbContext _stayAccessDbContext;
        private readonly IReservationService _reservationService;
        private readonly IGenericService<Reservation> _reservationRepo;
        private readonly IUsedBackupCodesService _usedBackupCodesService;
        private readonly ILogService _logService;

        public BackupCodesService(IConfiguration configuration, StayAccessDbContext stayAccessDbContext, IReservationService reservationService,
            IGenericService<Reservation> reservationRepo, IUsedBackupCodesService usedBackupCodesService, ILogService logService)
        {
            _configuration = configuration;
            _amountOfBackupsPerBuildingUnit = int.Parse(_configuration["BackupReservationsPerBuildingUnit"]);
            _stayAccessDbContext = stayAccessDbContext;
            _reservationService = reservationService;
            _reservationRepo = reservationRepo;
            _usedBackupCodesService = usedBackupCodesService;
            _logService = logService;
        }

        public async Task HandleArchesBackups(string loggedInUserName)
        {
            try
            {
                _logService.LogMessage($"In BackupCodesService HandleArchesBackups. loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);
                var (buildingUnitIdsUsingArchesLockSystem, archesBackupReservationsIQueryable) = GetArchesInfo();

                List<IGrouping<int, Reservation>> groupedArchesBackupReservationsByBuildingUnit = archesBackupReservationsIQueryable
                                                                                                  .AsEnumerable()
                                                                                                  .GroupBy(x => x.BuildingUnitId).ToList();

                _logService.LogMessage($"In BackupCodesService HandleArchesBackups. " +
                                       $" groupedArchesBackupReservationsByBuildingUnit: {groupedArchesBackupReservationsByBuildingUnit.ToJsonString()}." +
                                       $" loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                List<int> buildingUnitIdsWithAtLeastOneBackup = groupedArchesBackupReservationsByBuildingUnit.Select(x => x.Key).ToList();

                _logService.LogMessage($"In BackupCodesService HandleArchesBackups. " +
                                      $" buildingUnitIdsWithAtLeastOneBackup: {buildingUnitIdsWithAtLeastOneBackup.ToJsonString()}." +
                                      $" loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                await ArchesBuildingUnitsWithAtLeastOneBackupAndAddBackupsIfNeccessaryAsync(groupedArchesBackupReservationsByBuildingUnit, loggedInUserName);


                List<int> buildingUnitIdsWithNoBackups = buildingUnitIdsUsingArchesLockSystem
                                                         .Where(x => !buildingUnitIdsWithAtLeastOneBackup.Contains(x)).ToList();

                await AddArchesBackupsForBuildingUnitWithoutAnyBackupsAsync(buildingUnitIdsWithNoBackups, loggedInUserName);
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService HandleArchesBackups. Exception: {ex.ToJsonString()}. loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        private (List<int> buildingUnitIdsUsingArchesLockSystem, IQueryable<Reservation> archesBackupReservations) GetArchesInfo()
        {
            try
            {
                _logService.LogMessage($"In BackupCodesService GetArchesInfo.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                List<int> buildingIdsUsingArchesLockSystem = _stayAccessDbContext.Building
                                                                                     .Where(x => x.BuildingLockSystem == BuildingLockSystem.Arches)
                                                                                     .Select(x => x.Id).ToList();


                List<int> buildingUnitIdsUsingArchesLockSystem = _stayAccessDbContext.BuildingUnit
                                                                                     .Where(x => buildingIdsUsingArchesLockSystem.Contains(x.BuildingId))
                                                                                     .Select(x => x.Id).ToList();


                IQueryable<Reservation> archesBackupReservations = _stayAccessDbContext.Reservation
                                               .Where(x => x.Code.ToLower().Contains("backup")
                                               && buildingUnitIdsUsingArchesLockSystem.Contains(x.BuildingUnitId));

                _logService.LogMessage($"In BackupCodesService GetArchesInfo." +
                                       $" buildingIdsUsingArchesLockSystem: {buildingIdsUsingArchesLockSystem.ToJsonString()}." +
                                       $" buildingUnitIdsUsingArchesLockSystem: {buildingUnitIdsUsingArchesLockSystem.ToJsonString()}." +
                                       $" archesBackupReservations: {archesBackupReservations.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                return (buildingUnitIdsUsingArchesLockSystem, archesBackupReservations);
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in GetArchesInfo. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        private async Task AddArchesBackupsForBuildingUnitWithoutAnyBackupsAsync(List<int> buildingUnitIds, string loggedInUserName)
        {
            try
            {
                _logService.LogMessage($"In BackupCodesService AddArchesBackupsForBuildingUnitWithoutAnyBackupsAsync." +
                                       $" buildingUnitIds: {buildingUnitIds.ToJsonString()}." +
                                       $" loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                List<int> backupNumbersNeedToAdd = Enumerable.Range(1, _amountOfBackupsPerBuildingUnit).ToList();
                foreach (int buildingUnitId in buildingUnitIds)
                {
                    await AddBackupsForArchesBuildingUnit(buildingUnitId, backupNumbersNeedToAdd, loggedInUserName);
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService AddArchesBackupsForBuildingUnitWithoutAnyBackupsAsync. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        private async Task ArchesBuildingUnitsWithAtLeastOneBackupAndAddBackupsIfNeccessaryAsync
        (List<IGrouping<int, Reservation>> groupedArchesBackupReservationsByBuildingUnit, string loggedInUserName)
        {
            try
            {
                _logService.LogMessage($"In BackupCodesService ArchesBuildingUnitsWithAtLeastOneBackupAndAddBackupsIfNeccessaryAsync. loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);
                foreach (IGrouping<int, Reservation> archesBackupReservationIGroup in groupedArchesBackupReservationsByBuildingUnit)
                {
                    int buildingUnitId = archesBackupReservationIGroup.Key;

                    IEnumerable<Reservation> backupReservationsOfBuildingUnit = archesBackupReservationIGroup.Select(x => x);


                    List<Reservation> notUsedBackups = backupReservationsOfBuildingUnit.Where(x => !x.Code.Contains("*")).ToList();

                    List<Reservation> inactiveUnusedBackups = notUsedBackups.Where(x => (x.Cancelled || DateTime.Compare(currentEstDateTime, x.EndDate) > 0)).ToList();

                    int amountActiveUnusedBackups = notUsedBackups.Count - inactiveUnusedBackups.Count;


                    List<Reservation> backupsMarkedAsUsed = backupReservationsOfBuildingUnit.Where(x => x.Code.Contains("*")).ToList();

                    List<Reservation> backupsCurrentlyInUse = backupsMarkedAsUsed.Where(x => !x.Cancelled //not canceled
                                                                              && DateTime.Compare(currentEstDateTime, x.EndDate) <= 0)//and the reservation ends today or later
                                                                              .ToList();

                    _logService.LogMessage($"In BackupCodesService ArchesBuildingUnitsWithAtLeastOneBackupAndAddBackupsIfNeccessaryAsync." +
                                           $" buildingUnitId: {buildingUnitId}." +
                                           $" backupReservationsOfBuildingUnit: {backupReservationsOfBuildingUnit.ToJsonString()}." +
                                           $" notUsedBackups: {notUsedBackups.ToJsonString()}." +
                                           $" inactiveUnusedBackups: {inactiveUnusedBackups.ToJsonString()}." +
                                           $" amountActiveUnusedBackups: {amountActiveUnusedBackups}." +
                                           $" backupsMarkedAsUsed: {backupsMarkedAsUsed.ToJsonString()}." +
                                           $" backupsCurrentlyInUse: {backupsCurrentlyInUse.ToJsonString()}." +
                                           $" loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                    if (amountActiveUnusedBackups < _amountOfBackupsPerBuildingUnit)
                    {
                        int inactiveMarkedAsUsedBackupsAmount = backupsMarkedAsUsed.Count - backupsCurrentlyInUse.Count;

                        //var highestCurrentBackupNumber = backupReservationsOfBuildingUnit.Select(x => int.Parse(x.Code.Replace("*","").Split('#', ',')[1])).Max();


                        var highestCurrentBackupNumber = backupReservationsOfBuildingUnit.Select(x => int.Parse( new String( x.Code.Where(Char.IsDigit).ToArray() ) )).Max();

                        int count = _amountOfBackupsPerBuildingUnit - amountActiveUnusedBackups;
                        List<int> backupNumbersToAdd = Enumerable.Range(highestCurrentBackupNumber + 1, count).ToList();

                        //List<int> backupNumbersMissingFromThisUnit = Enumerable.Range(1, (_amountOfBackupsPerBuildingUnit + inactiveMarkedAsUsedBackupsAmount + inactiveUnusedBackups.Count))
                        //                                             //range from 1 through amount of backups there will be after this one is added
                        //                                             .Except(backupReservationsOfBuildingUnit.Select(x => int.Parse(x.Code.Split('#', ',')[1])).ToList()).ToList();

                        ////handle backupNumbersToAdd if building unit backups have double of one number (example backup#2 and backup#2 then the backupNumbersToAdd will be more than expected
                        //List<int> backupNumbersToAdd = backupNumbersMissingFromThisUnit.Take(_amountOfBackupsPerBuildingUnit - (amountActiveUnusedBackups + backupsCurrentlyInUse.Count)).ToList();

                        //_logService.LogMessage($"In BackupCodesService ArchesBuildingUnitsWithAtLeastOneBackupAndAddBackupsIfNeccessaryAsync." +
                        //                       $" inactiveMarkedAsUsedBackupsAmount: {inactiveMarkedAsUsedBackupsAmount.ToJsonString()}." +
                        //                       $" backupNumbersMissingFromThisUnit: {backupNumbersMissingFromThisUnit.ToJsonString()}." +
                        //                       $" backupNumbersToAdd: {backupNumbersToAdd.ToJsonString()}." +
                        //                       $" buildingUnitId: {buildingUnitId}." +
                        //                       $" backupReservationsOfBuildingUnit: {backupReservationsOfBuildingUnit.ToJsonString()}." +
                        //                       $" notUsedBackups: {notUsedBackups.ToJsonString()}." +
                        //                       $" inactiveUnusedBackups: {inactiveUnusedBackups.ToJsonString()}." +
                        //                       $" amountActiveUnusedBackups: {amountActiveUnusedBackups}." +
                        //                       $" backupsMarkedAsUsed: {backupsMarkedAsUsed.ToJsonString()}." +
                        //                       $" backupsCurrentlyInUse: {backupsCurrentlyInUse.ToJsonString()}." +
                        //                       $" loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);


                        _logService.LogMessage($"In BackupCodesService ArchesBuildingUnitsWithAtLeastOneBackupAndAddBackupsIfNeccessaryAsync." +
                                          $" inactiveMarkedAsUsedBackupsAmount: {inactiveMarkedAsUsedBackupsAmount.ToJsonString()}. " +
                                          $" _amountOfBackupsPerBuildingUnit: {_amountOfBackupsPerBuildingUnit}" +
                                          $" backupNumbersToAdd: {backupNumbersToAdd.ToJsonString()}." +
                                          $" buildingUnitId: {buildingUnitId}." +
                                          $" backupReservationsOfBuildingUnit: {backupReservationsOfBuildingUnit.ToJsonString()}." +
                                          $" notUsedBackups: {notUsedBackups.ToJsonString()}." +
                                          $" inactiveUnusedBackups: {inactiveUnusedBackups.ToJsonString()}." +
                                          $" amountActiveUnusedBackups: {amountActiveUnusedBackups}." +
                                          $" backupsMarkedAsUsed: {backupsMarkedAsUsed.ToJsonString()}." +
                                          $" backupsCurrentlyInUse: {backupsCurrentlyInUse.ToJsonString()}." +
                                          $" loggedInUserName: {loggedInUserName}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                        await AddBackupsForArchesBuildingUnit(buildingUnitId, backupNumbersToAdd, loggedInUserName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService ArchesBuildingUnitsWithAtLeastOneBackupAndAddBackupsIfNeccessaryAsync. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        private async Task<List<int>> AddBackupsForArchesBuildingUnit(int buildingUnitId, List<int> backupNumbersToAdd, string loggedInUserName)
        {
            try
            {
                _logService.LogMessage($"In BackupCodesService AddBackupsForArchesBuildingUnit." +
                    $" buildingUnitId:{buildingUnitId}." +
                    $" backupNumbersToAdd: {backupNumbersToAdd.ToJsonString()}." +
                    $" loggedInUserName: {loggedInUserName}.",
                    null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                List<int> addedBackupIds = new();
                foreach (int backupNumber in backupNumbersToAdd)
                {
                    ReservationWithCodesDto reservationWithCodesDto = new()
                    {
                        Id = default,
                        BuildingUnitId = buildingUnitId,
                        Code = $"Backup #{backupNumber}",
                        StartDate = currentEstDateTime,
                        EndDate = _endDate,
                        EarlyCheckIn = null,
                        LateCheckOut = null,
                        Cancelled = false,
                        ReservationCodes = new()
                        {
                            new()
                            {
                                LockCode = GetRandomLockCode(),
                            }
                        }
                    };

                    _logService.LogMessage($"In BackupCodesService AddBackupsForArchesBuildingUnit." +
                                           $" reservationWithCodesDto: {reservationWithCodesDto.ToJsonString()}" +
                                           $" buildingUnitId:{buildingUnitId}." +
                                           $" backupNumbersToAdd: {backupNumbersToAdd.ToJsonString()}." +
                                           $" loggedInUserName: {loggedInUserName}.",
                                           null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                    ReservationCodeResponseDto reservationCodeResponseDto = new();

                    ReservationCodeResponseDto responseDto = new();
                    reservationCodeResponseDto = await _reservationService.AddWithCodes(reservationWithCodesDto, responseDto, true, loggedInUserName);


                    _logService.LogMessage($"In BackupCodesService AddBackupsForArchesBuildingUnit." +
                                           $" reservationCodeResponseDto: {reservationCodeResponseDto.ToJsonString()}" +
                                           $" reservationWithCodesDto: {reservationWithCodesDto.ToJsonString()}" +
                                           $" buildingUnitId:{buildingUnitId}." +
                                           $" backupNumbersToAdd: {backupNumbersToAdd.ToJsonString()}." +
                                           $" loggedInUserName: {loggedInUserName}.",
                                           null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                    if (reservationCodeResponseDto?.ReservationId != default)
                    {
                        addedBackupIds.Add(reservationCodeResponseDto.ReservationId);
                    }
                }

                _logService.LogMessage($"In BackupCodesService AddBackupsForArchesBuildingUnit." +
                                       $" addedBackupIds: {addedBackupIds.ToJsonString()}.",
                                       null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);
                return addedBackupIds;
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService AddBackupsForArchesBuildingUnit. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        private string GetRandomLockCode()
        {
            try
            {
                Random generator = new Random();
                string randomLockCode = generator.Next(0, 1000000).ToString("D6");
                return randomLockCode;
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService GetRandomLockCode. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        private async Task UpdateArchesBackupReservation(string loggedInUserName, ReservationRequestDto reservationRequestDto)
        {
            try
            {
                _logService.LogMessage($"In BackupCodesService UpdateArchesBackupReservation." +
                                       $" loggedInUserName: {loggedInUserName}." +
                                       $" reservationRequestDto: {reservationRequestDto.ToJsonString()}.",
                                       null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);


                if (!_usedBackupCodesService.IsArchesBackupReservation(reservationRequestDto.Id).Result.isValid)
                {
                    throw new Exception("The reservationId is not a valid Arches reservation");
                }
                else
                {
                    _logService.LogMessage($"In BackupCodesService UpdateArchesBackupReservation." +
                                           $" Going into UpdateAsync." +
                                           $" loggedInUserName: {loggedInUserName}." +
                                           $" reservationRequestDto: {reservationRequestDto.ToJsonString()}.",
                                           null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                    await _reservationService.UpdateAsync(reservationRequestDto, loggedInUserName, BuildingLockSystem.Arches);
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService UpdateArchesBackupReservation. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        public DataSourceResultDto PagedList(FetchRequestDto request)
        {
            try
            {
                IQueryable<Reservation> query = _stayAccessDbContext.Reservation.Include(x => x.BuildingUnit.Building).Include(x => x.ReservationCodes).Include(x => x.ReservationLatchData).Where(x => x.Code.ToLower().Contains("backup"));

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
                           // LatchReservationToken = reservation.ReservationLatchData?.LatchReservationToken,
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
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService PagedList. Request: {request.ToJsonString()}. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        public async Task MarkAsUsedAsync(int backupReservationId, string triggeredBy)
        {
            try
            {
                _logService.LogMessage($"In BackupCodesService MarkAsUsedAsync.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                var (isValid, reservation) = await _usedBackupCodesService.IsArchesBackupReservation(backupReservationId);

                _logService.LogMessage($"In BackupCodesService MarkAsUsedAsync." +
                    $" isValid: {isValid}." +
                    $" reservation: {reservation.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                if (!isValid)
                {
                    throw new Exception($"Invalid Backup reservationId. Id: {backupReservationId}");
                }

                ReservationRequestDto reservationRequestDto = _usedBackupCodesService.GetBackupReservationDtoToSetAsUsed(reservation);

                _logService.LogMessage($"In BackupCodesService MarkAsUsedAsync." +
                                       $" reservationRequestDto: {reservationRequestDto.ToJsonString()}." +
                                       $" isValid: {isValid}." +
                                       $" reservation: {reservation.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                await UpdateArchesBackupReservation(triggeredBy, reservationRequestDto);

            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in BackupCodesService MarkAsUsedAsync. Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }
    }
}