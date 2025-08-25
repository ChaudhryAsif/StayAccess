using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Interfaces
{
    public interface IBuildingLockSystemService
    {
        Task<BuildingLockSystem> GetBuildingLockSystem(Reservation reservation);
        Task<BuildingLockSystem> GetBuildingLockSystemByBuildingUnitId(int buildingUnitId);
        Task<BuildingLockSystem> GetBuildingLockSystemByReservationId(int buildingUnitId);
    }
}
