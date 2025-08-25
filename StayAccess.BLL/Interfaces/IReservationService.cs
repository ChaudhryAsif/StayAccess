using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.ReservationCode;
using StayAccess.DTO.Reservations;
using StayAccess.DTO.Responses.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface IReservationService
    {
        Task<ReservationCodeResponseDto> AddAsync(ReservationRequestDto reservationDto, string userName, BuildingLockSystem newUnitBuildingLockSystem, bool isBackupReservation, List<ReservationCodeDto> reservationCodes = null);

        Task UpdateAsync(ReservationRequestDto reservationDto, string userName, BuildingLockSystem buildingLockSystem, bool isCronJob = false);

        Task DeleteAsync(int reservationId, string userName);

        Task<DAL.DomainEntities.Reservation> GetByIdAsync(int reservationId);

        Task<DAL.DomainEntities.Reservation> GetAsync(Expression<Func<DAL.DomainEntities.Reservation, bool>> predicate, params Expression<Func<DAL.DomainEntities.Reservation, object>>[] includes);

        Task CancelledAsync(int reservationId, string userName);

        Task EarlyCheckInAsync(int reservationId, string userName);

        Task LateCheckOutAsync(int reservationId, string userName);

        Task<DAL.DomainEntities.Reservation> GetMatchedReservationAsync(ReservationRequestDto reservationDto, BuildingLockSystem newUnitBuildingLockSystem, bool isCronJob = false);

        Task<List<DAL.DomainEntities.Reservation>> ListAsync(Expression<Func<DAL.DomainEntities.Reservation, bool>> predicate);

        DataSourceResultDto PagedList(FetchRequestDto request);

        IQueryable<DAL.DomainEntities.Reservation> GetIQueryableReservations(Expression<Func<DAL.DomainEntities.Reservation, bool>> predicate = null);
        Task<BuildingLockSystem> AdjustReservationAddWithCodesDto(ReservationWithCodesDto reservationDto);
        void AdjustReservationCodeDto(ReservationCodeDto reservationCodeDto, BuildingLockSystem reservationBuildingLockSystem);
        Task<BuildingLockSystem> AdjustReservationRequestDto(ReservationRequestDto reservationDto);
        Task<ReservationCodeResponseDto> AddWithCodes(ReservationWithCodesDto reservationDto, ReservationCodeResponseDto responseDto, bool isBackupReservation, string userName);
        Task<List<CurrentReservationResponseDto>> GetAllArchesCurrentReservationsAsync();
        ReservationMCData GetFirstOrDefault(int reservationId);
        Task<HttpStatusCode> AddMCReservationAsync(Reservation reservation, string userName);
    }
}
