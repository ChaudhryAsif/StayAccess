using StayAccess.DAL.ComplexEntities;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request;
using StayAccess.DTO.UnitLog;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface IBuildingUnitService
    {
        /// <summary>
        /// Add building unit
        /// </summary>
        /// <param name="buildingUnitDto"></param>
        Task<BuildingUnit> AddAsync(BuildingUnitRequestDto buildingUnitDto, string userName);

        /// <summary>
        /// Update building unit
        /// </summary>
        /// <param name="buildingUnitDto"></param>
        /// <returns></returns>
        Task UpdateAsync(BuildingUnitRequestDto buildingUnitDto, string userName);

        /// <summary>
        /// Delete building unit
        /// </summary>
        /// <param name="buildingUnitId"></param>
        /// <returns></returns>
        Task DeleteAsync(int buildingUnitId, string userName);

        /// <summary>
        ///  Get by id building unit
        /// </summary>
        /// <param name="buildingUnitId"></param>
        /// <returns></returns>
        Task<DAL.DomainEntities.BuildingUnit> GetByIdAsync(int buildingUnitId);

        /// <summary>
        /// Paged list of building unit
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        DataSourceResultDto PagedList(FetchRequestDto request);

        /// <summary>
        /// Get dropdown building units
        /// </summary>
        /// <returns></returns>
        Task<List<DropdownDto>> GetDropdownAsync(FetchRequestDto request);

        Task<List<DropdownDto>> GetDropdownAsync();

        Task<DataSourceResultDto> GetPageListUnitLogs(FetchRequestDto request);

        Task CaptureCodesAsync(List<CaptureCodesRequestDto> requestDtos);
    }
}