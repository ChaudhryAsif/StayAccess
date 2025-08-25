using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface IReservationCodeService
    {
        /// <summary>
        /// Add reservation code
        /// </summary>
        /// <param name="reservationCodeDto"></param>
        Task<ReservationCode> AddAsync(ReservationCodeDto reservationCodeDto, BuildingLockSystem buildingLockSystem, Reservation reservation, string userName);

        /// <summary>
        /// Get Reservation Code List
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<List<ReservationCode>> ListAsync(Expression<Func<ReservationCode, bool>> predicate);

        Task<List<ReservationCode>> ListByReservationIdAsync(int reservationId);

        Task<List<int>> BulkAddAsync(int reservationId, List<ReservationCodeDto> reservationCodeDtos, string userName, BuildingLockSystem buildingLockSystem);

        /// <summary>
        /// Update reservation code
        /// </summary>
        /// <param name="reservationCodeDto"></param>
        /// <returns></returns>
        Task UpdateAsync(ReservationCodeDto reservationCodeDto, string userName);

        /// <summary>
        /// update reservation code entity
        /// </summary>
        /// <param name="reservationCode"></param>
        void UpdateEntity(ReservationCode reservationCode);

        Task UpdateCodesStatusToPendingAsync(List<int> ids);

        /// <summary>
        /// Delete reservation code
        /// </summary>
        /// <param name="reservationCode"></param>
        /// <returns></returns>
        void Delete(ReservationCode reservationCode);

        void DeleteWithoutSave(Expression<Func<ReservationCode, bool>> predicate);

        /// <summary>
        /// Get by id reservation code
        /// </summary>
        /// <param name="reservationCodeId"></param>
        /// <returns></returns>
        Task<ReservationCode> GetByIdAsync(int reservationCodeId);

        /// <summary>
        /// Paged list of reservation codes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        DataSourceResultDto PagedList(FetchRequestDto request);

        /// <summary>
        /// get all reservation codes against provided filter
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IQueryable<ReservationCode> List(Expression<Func<ReservationCode, bool>> predicate);
    }
}
