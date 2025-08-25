using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request;
using StayAccess.DTO.Request.Latch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface IBuildingService
    {
        /// <summary>
        /// add building
        /// </summary>
        /// <param name="buildingDto"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public Building Add(BuildingRequestDto buildingDto, string userName);

        /// <summary>
        /// update building
        /// </summary>
        /// <param name="buildingRequestDto"></param>
        /// <param name="getLoggedInUserName"></param>
        /// <returns></returns>
        public Task UpdateAsync(BuildingRequestDto buildingRequestDto, string getLoggedInUserName);

        /// <summary>
        /// delete building
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task DeleteAsync(int buildingId, string userName);

        /// <summary>
        /// get by id building
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        Task<Building> GetByIdAsync(int buildingId);

        /// <summary>
        /// get matching building record from database
        /// </summary>
        /// <param name="buildingRequestDto"></param>
        /// <returns></returns>
        Task<Building> GetMatchedBuildingAsync(BuildingRequestDto buildingRequestDto);
        DataSourceResultDto PagedList(FetchRequestDto request);
        Task<List<DropdownDto>> GetDropdownAsync();
    }
}
