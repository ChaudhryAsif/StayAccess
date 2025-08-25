using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Repositories
{
    public class BuildingLockSystemService : IBuildingLockSystemService
    {
        private readonly IGenericService<BuildingUnit> _buildingUnitRepo;
        private readonly IGenericService<Reservation> _reservationRepo;

        public BuildingLockSystemService(IGenericService<BuildingUnit> buildingUnitRepo, IGenericService<Reservation> reservationRepo)
        {
            _buildingUnitRepo = buildingUnitRepo;
            _reservationRepo = reservationRepo;        }

        public async Task<BuildingLockSystem> GetBuildingLockSystem(Reservation reservation)
        {
            return await GetBuildingLockSystemByBuildingUnitId(reservation.BuildingUnitId);
        }

        public async Task<BuildingLockSystem> GetBuildingLockSystemByBuildingUnitId(int buildingUnitId)
        {
            try
            {
                BuildingUnit buildingUnit = await _buildingUnitRepo.GetAsync(x => x.Id == buildingUnitId, a => a.Building);
                if (buildingUnit == null)
                    throw new Exception($"Building unit not found. BuildingUnitId: {buildingUnitId}");
                return buildingUnit.Building.BuildingLockSystem;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<BuildingLockSystem> GetBuildingLockSystemByReservationId(int reservationId)
        {
            try
            {
                Reservation reservation = await _reservationRepo.GetAsync(x => x.Id == reservationId, a => a.BuildingUnit.Building);
                return reservation.BuildingUnit.Building.BuildingLockSystem;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
