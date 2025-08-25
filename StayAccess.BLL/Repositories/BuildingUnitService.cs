using Microsoft.EntityFrameworkCore;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request;
using StayAccess.DTO.UnitLog;
using StayAccess.Tools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StayAccess.BLL.Repositories
{
    public class BuildingUnitService : IBuildingUnitService
    {
        private readonly IGenericService<DAL.DomainEntities.BuildingUnit> _buildingUnitRepo;
        private readonly IGenericService<Building> _buildingRepo;
        private readonly IReservationService _reservationRepo;
        

        private readonly IGenericService<UnitSlotLog> _unitSlotLogRepo;
        private readonly IGenericService<UnitLog> _unitLogRepo;

        public BuildingUnitService(IGenericService<DAL.DomainEntities.BuildingUnit> buildingUnitRepo,
            IGenericService<Building> buildingRepo,
            IReservationService reservationRepo,
            IGenericService<UnitLog> unitLogRepo, IGenericService<UnitSlotLog> unitSlotRepo)
        {
            _buildingUnitRepo = buildingUnitRepo;
            _buildingRepo = buildingRepo;
            _reservationRepo = reservationRepo;
            _unitSlotLogRepo = unitSlotRepo;
            _unitLogRepo = unitLogRepo;
        }

        public async Task<BuildingUnit> AddAsync(BuildingUnitRequestDto buildingUnitDto, string userName)
        {
            try
            {
                Building building = await GetBuildingByBuildingIdAsync(buildingUnitDto);
                if (building is null)
                    throw new Exception("Building not found.");

                BuildingUnit buildingUnit = new BuildingUnit
                {
                    Id = buildingUnitDto.Id,
                    BuildingId = building.Id,
                    UnitId = buildingUnitDto.UnitId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userName,
                };
                _buildingUnitRepo.AddWithSave(buildingUnit);
                return buildingUnit;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<Building> GetBuildingByBuildingIdAsync(BuildingUnitRequestDto buildingUnitDto)
        {
            try
            {
                 return await _buildingRepo.GetAsync(x => x.Id == buildingUnitDto.BuildingId);
                
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateAsync(BuildingUnitRequestDto buildingUnitDto, string userName)
        {
            try
            {
                Building building = await GetBuildingByBuildingIdAsync(buildingUnitDto);
                if(building is null)
                    throw new Exception("Building not found.");

                DAL.DomainEntities.BuildingUnit buildingUnit = await GetByIdAsync(buildingUnitDto.Id);

                if (buildingUnit is null)
                    throw new Exception("Building Unit not found.");

                buildingUnit.Id = buildingUnitDto.Id;
                buildingUnit.BuildingId = building.Id;
                buildingUnit.UnitId = buildingUnitDto.UnitId;
                buildingUnit.ModifiedDate = DateTime.UtcNow;
                buildingUnit.ModifiedBy = userName;

                _buildingUnitRepo.UpdateWithSave(buildingUnit);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int buildingUnitId, string userName)
        {
            try
            {
                DAL.DomainEntities.BuildingUnit buildingUnit = await GetByIdAsync(buildingUnitId);

                if (buildingUnit is null)
                    throw new Exception("Building Unit not found.");

                var reservations = await _reservationRepo.GetIQueryableReservations(x => x.BuildingUnitId == buildingUnitId).ToListAsync();
                foreach (var reservation in reservations)
                {
                    await _reservationRepo.DeleteAsync(reservation.Id, userName);
                }

                _buildingUnitRepo.DeleteWithSave(buildingUnit);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<DAL.DomainEntities.BuildingUnit> GetByIdAsync(int buildingUnitId)
        {
            try
            {
                return await _buildingUnitRepo.GetAsync(x => x.Id == buildingUnitId);
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
                IQueryable<DAL.DomainEntities.BuildingUnit> query = GetIQueryableBuildingUnit();

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

                            if (field.Equals("buildingid"))
                                query = query.Where(x => x.BuildingId.ToString().Equals(value));

                            if (field.Equals("unitid"))
                                query = query.Where(x => x.UnitId.ToLower().Contains(value));
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
                            case "buildingid":
                                query = isAscending ? query.OrderBy(x => x.BuildingId) : query.OrderByDescending(x => x.BuildingId);
                                break;
                            case "unitid":
                                query = isAscending ? query.OrderBy(x => x.UnitId) : query.OrderByDescending(x => x.UnitId);
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

                return _buildingUnitRepo.PagedList(query, request.Page, request.PageSize);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<DropdownDto>> GetDropdownAsync(FetchRequestDto request)
        {
            try
            {
                IQueryable<BuildingUnit> query = _buildingUnitRepo.List();
                if (request.Filters.HasAny())
                {
                    foreach (FilterDto filter in request.Filters)
                    {
                        if (!string.IsNullOrEmpty(filter.Field))
                        {
                            string field = filter.Field.ToLower();
                            string value = !string.IsNullOrEmpty(filter.Value) ? filter.Value.ToLower() : string.Empty;

                            if (field.Equals("buildingid"))
                                query = query.Where(x => x.BuildingId.ToString().Equals(value));

                            if (field.Equals("unitid"))
                                query = query.Where(x => x.UnitId.ToString().Equals(value));
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
                            case "buildingid":
                                query = isAscending ? query.OrderBy(x => x.BuildingId) : query.OrderByDescending(x => x.BuildingId);
                                break;
                            case "unitid":
                                query = isAscending ? query.OrderBy(x => x.UnitId) : query.OrderByDescending(x => x.UnitId);
                                break;
                        }
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id);
                }

                return await (from buildingUnit in query
                              select new DropdownDto
                              {
                                  Value = buildingUnit.Id.ToString(),
                                  Label = buildingUnit.UnitId,
                              }).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<DropdownDto>> GetDropdownAsync()
        {
            try
            {
                IQueryable<BuildingUnit> query = _buildingUnitRepo.List();
                return await (from buildingUnit in query
                              select new DropdownDto
                              {
                                  Value = buildingUnit.Id.ToString(),
                                  Label = buildingUnit.UnitId,
                              }).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private IQueryable<DAL.DomainEntities.BuildingUnit> GetIQueryableBuildingUnit(Expression<Func<DAL.DomainEntities.BuildingUnit, bool>> predicate = null)
        {
            try
            {
                return _buildingUnitRepo.List(predicate);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<DataSourceResultDto> GetPageListUnitLogs(FetchRequestDto request)
        {
            try
            {
                List<UnitLogResponseDto> unitLogResponseDtos = new();
                var unitLogs = await _unitLogRepo.List().ToListAsync();
                if (unitLogs.HasAny())
                {
                    List<string> unitsList = unitLogs.Select(x => x.Unit).Distinct().ToList();
                    List<UnitSlotLog> unitSlotLogs = await _unitSlotLogRepo.List(x => unitsList.Contains(x.Unit)).ToListAsync();
                    foreach (var unitLog in unitLogs)
                    {
                        List<UnitSlotLog> matchedUnitSlotLogs = unitSlotLogs.Where(x => x.Unit == unitLog.Unit).ToList();
                        foreach (var matchedUnitSlotLog in matchedUnitSlotLogs)
                        {
                            unitLogResponseDtos.Add(new UnitLogResponseDto
                            {
                                UnitLogId = unitLog.Id,
                                BatteryLevel = Convert.ToInt32(unitLog.BatteryLevel),
                                UnitSlotLogId = matchedUnitSlotLog.Id,
                                Code = Convert.ToInt32(matchedUnitSlotLog.Code),
                                Slot = Convert.ToInt32(matchedUnitSlotLog.Slot),
                                Unit = matchedUnitSlotLog.Unit
                            });
                        }
                    }
                }

                IQueryable<UnitLogResponseDto> querable = unitLogResponseDtos.AsQueryable();

                if (request.Filters.HasAny())
                {
                    foreach (FilterDto filter in request.Filters)
                    {
                        if (!string.IsNullOrEmpty(filter.Field))
                        {
                            string field = filter.Field.ToLower();
                            string value = !string.IsNullOrEmpty(filter.Value) ? filter.Value.ToLower() : string.Empty;

                            if (field.Equals("unitlogid"))
                                querable = querable.Where(x => x.UnitLogId.ToString().Equals(value));

                            if (field.Equals("batterylevel"))
                                querable = querable.Where(x => x.BatteryLevel.ToString().Equals(value));

                            if (field.Equals("unitslotlogid"))
                                querable = querable.Where(x => x.UnitSlotLogId.ToString().Contains(value));

                            if (field.Equals("code"))
                                querable = querable.Where(x => x.Code.ToString().Contains(value));

                            if (field.Equals("slot"))
                                querable = querable.Where(x => x.Slot.ToString().Contains(value));

                            if (field.Equals("unit"))
                                querable = querable.Where(x => x.Unit.Equals(value));
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
                            case "unitlogid":
                                querable = isAscending ? querable.OrderBy(x => x.UnitLogId) : querable.OrderByDescending(x => x.UnitLogId);
                                break;
                            case "unitslotlogid":
                                querable = isAscending ? querable.OrderBy(x => x.UnitSlotLogId) : querable.OrderByDescending(x => x.UnitSlotLogId);
                                break;
                            case "batterylevel":
                                querable = isAscending ? querable.OrderBy(x => x.BatteryLevel) : querable.OrderByDescending(x => x.BatteryLevel);
                                break;
                            case "slot":
                                querable = isAscending ? querable.OrderBy(x => x.Slot) : querable.OrderByDescending(x => x.Slot);
                                break;
                            case "unit":
                                querable = isAscending ? querable.OrderBy(x => x.Unit) : querable.OrderByDescending(x => x.Unit);
                                break;
                            case "code":
                                querable = isAscending ? querable.OrderBy(x => x.Code) : querable.OrderByDescending(x => x.Code);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    querable = querable.OrderByDescending(x => x.UnitLogId);
                }

                return Pagination(querable, request.Page, request.PageSize);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task CaptureCodesAsync(List<CaptureCodesRequestDto> requestDto)
        {
            try
            {
                foreach (var request in requestDto)
                {
                    string unit = request.name;

                    // "name": "arches_210a_door",
                    if (unit.Contains("_"))
                    {
                        var chars = unit.Split("_");
                        if (chars.Length == 3)
                            unit = chars[1];
                    }

                    // get existing UnitSlotLog records against unit
                    var existingUnitSlotLogs = _unitSlotLogRepo.List(x => x.Unit == unit);
                    if (existingUnitSlotLogs.HasAny())
                    {
                        // delete existing UnitSlotLog records against unit with save
                        _unitSlotLogRepo.DeleteRangeWithSave(existingUnitSlotLogs);
                    }

                    foreach (Value valueItem in request.values)
                    {
                        int value = valueItem.value;

                        if (valueItem.label.Contains("Battery level"))
                        {
                            UnitLog unitLog = await _unitLogRepo.GetAsync(x => x.Unit == unit);
                            if (unitLog is null)
                            {
                                _unitLogRepo.AddWithSave(new UnitLog
                                {
                                    NodeId = valueItem.nodeId,
                                    Unit = unit,
                                    BatteryLevel = value.ToString(),
                                    LastUpdated = valueItem.lastUpdate.ToString(),
                                    LastUpdatedTime = DateTimeOffset.FromUnixTimeMilliseconds(valueItem.lastUpdate).DateTime,
                                    HomeId = valueItem.homeId.ToString()
                                });
                            }
                            else
                            {
                                unitLog.NodeId = valueItem.nodeId;
                                unitLog.Unit = unit;
                                unitLog.BatteryLevel = value.ToString();
                                unitLog.LastUpdated = valueItem.lastUpdate.ToString();
                                unitLog.LastUpdatedTime = DateTimeOffset.FromUnixTimeMilliseconds(valueItem.lastUpdate).DateTime;
                                unitLog.HomeId = valueItem.homeId.ToString();
                                _unitLogRepo.UpdateWithSave(unitLog);
                            }
                        }
                        else if (valueItem.label.Contains("User Code"))
                        {
                            int slot = valueItem.propertyKey;
                            string code = value.ToString();

                            // prepend zero(s) if code isn't full length
                            if (!string.IsNullOrEmpty(code) && code.Length < 6)
                              code =  new string('0', 6 - code.Length) + code;

                            // adding new record for unit.
                            _unitSlotLogRepo.AddWithSave(new UnitSlotLog
                            {
                                NodeId = valueItem.nodeId,
                                Unit = unit,
                                Slot = slot.ToString(),
                                Code = code,
                                LastUpdate = valueItem.lastUpdate.ToString(),
                                LastUpdatedTime = DateTimeOffset.FromUnixTimeMilliseconds(valueItem.lastUpdate).DateTime
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        private DataSourceResultDto Pagination(IQueryable<UnitLogResponseDto> querable, int pageNo, int pageSize)
        {
            try
            {
                int skipRecords = pageNo <= 1 ? 0 : (pageNo - 1) * pageSize;
                int totalRecords = querable.Count();
                List<UnitLogResponseDto> paginatedResults = querable.Skip(skipRecords).Take(pageSize).ToList();
                DataSourceResultDto result = new()
                {
                    Data = paginatedResults.HasAny() ? paginatedResults : new(),
                    Total = totalRecords
                };
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}