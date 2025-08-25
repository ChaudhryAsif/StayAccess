using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Repositories
{
    public class BuildingService : IBuildingService
    {
        private readonly IGenericService<Building> _buildingRepo;
        private readonly ILoggerService<BuildingService> _loggerRepo;
        public BuildingService(IGenericService<DAL.DomainEntities.Building> buildingRepo, ILoggerService<BuildingService> loggerRepo)
        {
            _buildingRepo = buildingRepo;
            _loggerRepo = loggerRepo;
        }

        public Building Add(BuildingRequestDto buildingDto, string userName)
        {
            try
            {
                Building building = new Building
                {
                    Id = buildingDto.Id,
                    Name = buildingDto.Name,
                    BuildingLockSystem = buildingDto.BuildingLockSystem,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userName,
                };


                _loggerRepo.Add(LogType.Information, $"Creating new building: {JsonConvert.SerializeObject(building)}.", null);
                _buildingRepo.AddWithSave(building);
                _loggerRepo.Add(LogType.Information, $"New building created successfully for: {JsonConvert.SerializeObject(building)}.", null);

                return building;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task UpdateAsync(BuildingRequestDto buildingDto, string userName)
        {
            try
            {
                if (buildingDto.Id == default)
                    throw new Exception("Invalid BuildingId.");

                DAL.DomainEntities.Building building = await GetByIdAsync(buildingDto.Id);

                if (building is null)
                    throw new Exception("Building not found.");

                building.Id = buildingDto.Id;
                building.Name = buildingDto.Name;
                building.BuildingLockSystem = buildingDto.BuildingLockSystem;
                building.ModifiedDate = DateTime.UtcNow;
                building.ModifiedBy = userName;

                _loggerRepo.Add(LogType.Information, $"Updating building: {JsonConvert.SerializeObject(building)}.", null);
                _buildingRepo.UpdateWithSave(building);
                _loggerRepo.Add(LogType.Information, $"Building updated successfully for: {JsonConvert.SerializeObject(building)}.", null);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Building> GetByIdAsync(int buildingId)
        {
            try
            {
                return await _buildingRepo.GetAsync(x => x.Id == buildingId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int buildingId, string userName)
        {
            try
            {
                Building building = await GetByIdAsync(buildingId);

                if (building is null)
                    throw new Exception("Building not found.");


                _loggerRepo.Add(LogType.Information, $"Deleting building: {JsonConvert.SerializeObject(building)}.", null);
                _buildingRepo.DeleteWithSave(building);
                _loggerRepo.Add(LogType.Information, $"Building deleted successfully for: {JsonConvert.SerializeObject(building)}.", null);


            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Building> GetMatchedBuildingAsync(BuildingRequestDto buildingRequestDto)
        {
            try
            {
                return await _buildingRepo.GetAsync(x => x.Name == buildingRequestDto.Name
                                                         && x.BuildingLockSystem == buildingRequestDto.BuildingLockSystem);
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
                IQueryable<Building> buildings = _buildingRepo.List();
                return await (from building in buildings
                              select new DropdownDto
                              {
                                  Value = building.Id.ToString(),
                                  Label = building.Name,
                              }).ToListAsync();
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
                IQueryable<Building> query = _buildingRepo.List();

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

                            if (field.Equals("name"))
                                query = query.Where(x => x.Name.ToLower().Contains(value));

                            if (field.Equals("buildinglocksystem"))
                                query = query.Where(x => x.BuildingLockSystem.ToString().Equals(value));

                            if (field.Equals("createdby"))
                                query = query.Where(x => x.CreatedBy.ToLower().Contains(value));

                            if (field.Equals("createddate"))
                                query = query.Where(x => x.CreatedDate == Convert.ToDateTime(value).Date);

                            if (field.Equals("modifiedby"))
                                query = query.Where(x => x.ModifiedBy.ToLower().Contains(value));

                            if (field.Equals("modifieddate"))
                                query = query.Where(x => x.ModifiedDate == Convert.ToDateTime(value).Date);

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
                            case "name":
                                query = isAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name);
                                break;
                            case "buildinglocksystem":
                                query = isAscending ? query.OrderBy(x => x.BuildingLockSystem) : query.OrderByDescending(x => x.BuildingLockSystem);
                                break;
                            case "createdby":
                                query = isAscending ? query.OrderBy(x => x.CreatedBy) : query.OrderByDescending(x => x.CreatedBy);
                                break;
                            case "createddate":
                                query = isAscending ? query.OrderBy(x => x.CreatedDate) : query.OrderByDescending(x => x.CreatedDate);
                                break;
                            case "modifiedby":
                                query = isAscending ? query.OrderBy(x => x.ModifiedBy) : query.OrderByDescending(x => x.ModifiedBy);
                                break;
                            case "modifieddate":
                                query = isAscending ? query.OrderBy(x => x.ModifiedDate) : query.OrderByDescending(x => x.ModifiedDate);
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

                DataSourceResultDto dataSourceResult = _buildingRepo.PagedList(query, request.Page, request.PageSize);
                var buildings = (List<Building>)dataSourceResult.Data;
                return dataSourceResult;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
