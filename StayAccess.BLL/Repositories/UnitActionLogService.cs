using StayAccess.Tools.Interfaces;
using System;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Request;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Extensions;
using StayAccess.DTO;
using System.Linq;
using StayAccess.DAL;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using StayAccess.BLL.Interfaces;
using StayAccess.Tools;

namespace StayAccess.BLL.Repositories
{
    public class UnitActionLogService : IUnitActionLogService
    {
        private readonly IGenericService<UnitActionLog> _unitActionLogRepo;
        private readonly ILogService _logRepo;
        private readonly IGenericService<UnitLog> _unitLogRepo;
        private readonly IGenericService<BuildingUnit> _buildingUnitRepo;
        private readonly StayAccessDbContext _stayAccessDbContext;
        private readonly IBackupCodesService _backupCodesService;

        public UnitActionLogService(IGenericService<UnitActionLog> unitActionLogRepo, ILogService logRepo, IGenericService<UnitLog> unitLogRepo,
            IGenericService<BuildingUnit> buildingUnitRepo, StayAccessDbContext stayAccessDbContext, IBackupCodesService backupCodesService)
        {
            _unitActionLogRepo = unitActionLogRepo;
            _logRepo = logRepo;
            _unitLogRepo = unitLogRepo;
            _buildingUnitRepo = buildingUnitRepo;
            _stayAccessDbContext = stayAccessDbContext;
            _backupCodesService = backupCodesService;
        }

        public async Task<UnitActionLog> AddAsync(UnitActionLogRequestDto requestDto)
        {
            try
            {
                string unit = _unitLogRepo.Get(x => x.NodeId == requestDto.NodeId && x.HomeId == requestDto.InstallationId.ToString()).Unit;

                UnitActionLog unitActionLog = new()
                {
                    InstallationId = requestDto.InstallationId.ToString(),
                    NodeId = requestDto.NodeId,
                    Unit = unit,
                    DateTime = requestDto.DateTime,
                    EventCode = requestDto.EventCode,
                    Slot = requestDto.Slot
                };
                _unitActionLogRepo.AddWithSave(unitActionLog);

                _logRepo.LogMessage($"Unit action log was added to the database with save. requestDto: {requestDto.ToJsonString()}. unitActionLog: {unitActionLog.ToJsonString()}",
                    null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                BuildingUnit buildingUnit = _buildingUnitRepo.Get(x => x.UnitId == unit);

                List<Reservation> reservations = await _stayAccessDbContext.Reservation
                                                .Include(x => x.ReservationCodes.Where(x => x.SlotNo == int.Parse(requestDto.Slot)))
                                                .Where(x => x.BuildingUnitId == buildingUnit.Id).ToListAsync(); //should really always return one reservation

                _logRepo.LogMessage($"Unit action log. reservations: {reservations.ToJsonString()}. unitActionLog: {unitActionLog.ToJsonString()}",
                    null, false, Utilities.GetCurrentTimeInEST(), LogType.Information);

                if (reservations?.Count != 1)
                {
                    throw new Exception($"Expected 1 reservation, found {reservations?.Count}. (Unit Action Log Add Async)");
                }

                await _backupCodesService.MarkAsUsedAsync(reservations.Select(x => x.Id).FirstOrDefault(), "API Add Unit Action Log");

                return unitActionLog;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"An error occurred in adding UnitActionLog. Exception: {ex.ToJsonString()}", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }

        public DataSourceResultDto GetLogsFromRequest(FetchRequestDto request)
        {
            try
            {
                IQueryable<UnitActionLog> query = _unitActionLogRepo.List();

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

                            if (field.Equals("installationid"))
                                query = query.Where(x => x.InstallationId == value);

                            if (field.Equals("nodeid"))
                                query = query.Where(x => x.NodeId == int.Parse(value));

                            if (field.Equals("unit"))
                                query = query.Where(x => x.Unit == value);

                            if (field.Equals("datetime"))
                                query = query.Where(x => x.DateTime == Convert.ToDateTime(value).Date);

                            if (field.Equals("eventcode"))
                                query = query.Where(x => x.EventCode == value);

                            if (field.Equals("slot"))
                                query = query.Where(x => x.Slot == value);

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
                            case "installationid":
                                query = isAscending ? query.OrderBy(x => x.InstallationId) : query.OrderByDescending(x => x.InstallationId);
                                break;
                            case "nodeid":
                                query = isAscending ? query.OrderBy(x => x.NodeId) : query.OrderByDescending(x => x.NodeId);
                                break;
                            case "unit":
                                query = isAscending ? query.OrderBy(x => x.Unit) : query.OrderByDescending(x => x.Unit);
                                break;
                            case "datetime":
                                query = isAscending ? query.OrderBy(x => x.DateTime) : query.OrderByDescending(x => x.DateTime);
                                break;
                            case "eventcode":
                                query = isAscending ? query.OrderBy(x => x.EventCode) : query.OrderByDescending(x => x.EventCode);
                                break;
                            case "slot":
                                query = isAscending ? query.OrderBy(x => x.Slot) : query.OrderByDescending(x => x.Slot);
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

                return _unitActionLogRepo.PagedList(query, request.Page, request.PageSize);
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"An error occurred in getting UnitActionLogs. Request: {request.ToJsonString()}. Exception: {ex.ToJsonString()}", null, false, Utilities.GetCurrentTimeInEST(), LogType.Error);
                throw;
            }
        }
    }
}