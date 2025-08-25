using Microsoft.EntityFrameworkCore;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Reservations;
using StayAccess.Tools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StayAccess.BLL.Repositories
{
    public class ReservationCodeService : Interfaces.IReservationCodeService
    {
        private readonly StayAccessDbContext _stayAccessDbContext;
        private readonly IGenericService<ReservationCode> _reservationCodeRepo;
        private readonly ICodeTransactionService _codeTransactionRepo;

        public ReservationCodeService(StayAccessDbContext stayAccessDbContext, IGenericService<ReservationCode> reservationCodeRepo, ICodeTransactionService codeTransactionRepo)
        {
            _stayAccessDbContext = stayAccessDbContext;
            _reservationCodeRepo = reservationCodeRepo;
            _codeTransactionRepo = codeTransactionRepo;
        }

        #region public methods

        public async Task<ReservationCode> AddAsync(ReservationCodeDto reservationCodeDto, BuildingLockSystem buildingLockSystem, DAL.DomainEntities.Reservation reservation, string userName)
        {
            try
            {
                ReservationCode reservationCode = await SetReservationCodeEntity(reservationCodeDto, buildingLockSystem, reservationCodeDto.ReservationId, userName);
                _reservationCodeRepo.AddWithSave(reservationCode);

                // create transactions for newly geneated pending codes
                _codeTransactionRepo.CreatePendingCodeTransactionsForNew(reservation, userName, reservationCode.Id);

                // start executing pending transactions in background
                _codeTransactionRepo.ExecuteTransactions();

                return reservationCode;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<int>> BulkAddAsync(int reservationId, List<ReservationCodeDto> reservationCodeDtos, string userName, BuildingLockSystem buildingLockSystem)
        {
            try
            {
                var reservation = _stayAccessDbContext.Reservation.FirstOrDefault(x => x.Id == reservationId);

                CodeStatus? codeStatus = null;
                if (buildingLockSystem == BuildingLockSystem.Latch && reservation.Cancelled)
                    codeStatus = CodeStatus.Deleted;

                List<int> reservationCodeIds = new();
                foreach (var reservationCodeDto in reservationCodeDtos ?? new List<ReservationCodeDto>())
                {
                    ReservationCode reservationCode = await SetReservationCodeEntity(reservationCodeDto, buildingLockSystem, reservationId, userName, codeStatus);

                    _reservationCodeRepo.AddWithSave(reservationCode);
                    reservationCodeIds.Add(reservationCode.Id);
                }

                if (reservation != null && !reservation.Cancelled)
                {
                    //create transactions for newly generated pending codes
                    _codeTransactionRepo.CreatePendingCodeTransactionsForNew(reservation, userName);

                    // start executing pending transactions in background
                    _codeTransactionRepo.ExecuteTransactions(false);
                }

                return reservationCodeIds;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task UpdateAsync(ReservationCodeDto reservationCodeDto, string userName)
        {
            try
            {
                ReservationCode reservationCode = await GetByIdAsync(reservationCodeDto.Id);

                if (reservationCode is null)
                    throw new Exception("Reservation code not found.");

                reservationCode.Id = reservationCodeDto.Id;
                reservationCode.ReservationId = reservationCodeDto.ReservationId;
                reservationCode.LockCode = reservationCodeDto.LockCode;
                reservationCode.ModifiedDate = DateTime.UtcNow;
                reservationCode.ModifiedBy = userName;

                _reservationCodeRepo.UpdateWithSave(reservationCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void UpdateEntity(ReservationCode reservationCode)
        {
            try
            {
                _reservationCodeRepo.UpdateWithSave(reservationCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateCodesStatusToPendingAsync(List<int> ids)
        {
            try
            {
                foreach (var id in ids)
                {
                    await UpdateStatusToPendingAsync(id);
                }
                _reservationCodeRepo.Save();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Delete(ReservationCode reservationCode)
        {
            try
            {
                _reservationCodeRepo.DeleteWithSave(reservationCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteWithoutSave(Expression<Func<ReservationCode, bool>> predicate)
        {
            try
            {
                var reservationCodes = _reservationCodeRepo.List(predicate);
                foreach (var reservationCode in reservationCodes)
                {
                    _reservationCodeRepo.Delete(reservationCode);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ReservationCode> GetByIdAsync(int reservationCodeId)
        {
            try
            {
                return await _reservationCodeRepo.GetAsync(x => x.Id == reservationCodeId, a => a.Reservation);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<ReservationCode>> ListAsync(Expression<Func<ReservationCode, bool>> predicate)
        {
            try
            {
                return await _reservationCodeRepo.List(predicate, a => a.Reservation).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<ReservationCode>> ListByReservationIdAsync(int reservationId)
        {
            try
            {
                return await _reservationCodeRepo.List(x => x.ReservationId == reservationId).ToListAsync();
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
                IQueryable<ReservationCode> query = GetIQueryableReservationCode();

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

                            if (field.Equals("lockcode"))
                                query = query.Where(x => x.LockCode.ToLower().Equals(value));

                            if (field.Equals("slotno"))
                                query = query.Where(x => x.SlotNo.ToString().Equals(value));

                            if (field.Equals("status"))
                                query = query.Where(x => x.Status.Equals(Enum.Parse(typeof(CodeStatus), value)));

                            if (field.Equals("reservationid"))
                                query = query.Where(x => x.ReservationId.ToString().Equals(value));
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
                            case "lockcode":
                                query = isAscending ? query.OrderBy(x => x.LockCode) : query.OrderByDescending(x => x.LockCode);
                                break;
                            case "slotno":
                                query = isAscending ? query.OrderBy(x => x.SlotNo) : query.OrderByDescending(x => x.SlotNo);
                                break;
                            case "status":
                                query = isAscending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status);
                                break;
                            case "reservationid":
                                query = isAscending ? query.OrderBy(x => x.ReservationId) : query.OrderByDescending(x => x.ReservationId);
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

                return _reservationCodeRepo.PagedList(query, request.Page, request.PageSize);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// get all reservation codes against provided filter
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IQueryable<ReservationCode> List(Expression<Func<ReservationCode, bool>> predicate)
        {
            try
            {
                return _reservationCodeRepo.List(predicate, a => a.Reservation);
            }
            catch (Exception)
            {
                throw;
            }
        }

        //private async Task<int> GetSlotNumber(int reservationId)
        //{
        //    try
        //    {

        //        int defaultInt = 131;

        //        // get reservation info
        //        var reservation = await _stayAccessDbContext.Reservation.FirstOrDefaultAsync(x => x.Id == reservationId);

        //        if (reservation is null)
        //            return defaultInt;

        //        // get last reservation code
        //        ReservationCode lastReservationCode = _stayAccessDbContext.ReservationCode.Include(x => x.Reservation)
        //                                                                                  .Where(x => x.Reservation.BuildingUnitId == reservation.BuildingUnitId)
        //                                                                                  .OrderByDescending(x => x.Id).AsNoTracking().FirstOrDefault();
        //        if (lastReservationCode is null)
        //            return defaultInt;

        //        if (lastReservationCode.SlotNo == 199)
        //            return defaultInt;
        //        else
        //            return lastReservationCode.SlotNo += 1;
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        private async Task<int> GetSlotNumber(int reservationId)
        {
            try
            {
                // get reservation info
                var reservation = await _stayAccessDbContext.Reservation.FirstOrDefaultAsync(x => x.Id == reservationId);

                if (reservation is null)
                    return 131;

                // get last reservation code
                ReservationCode lastReservationCode = _stayAccessDbContext.ReservationCode.Include(x => x.Reservation)
                                                                                          .Where(x => x.Reservation.BuildingUnitId == reservation.BuildingUnitId)
                                                                                          .OrderByDescending(x => x.Id).AsNoTracking().FirstOrDefault();
                if (lastReservationCode is null)
                    return 131;

                if (lastReservationCode.SlotNo == 199)
                    return 131;
                else
                    return lastReservationCode.SlotNo += 1;
            }
            catch (Exception)
            {
                throw;
            }
        }


        #endregion

        #region private methods

        private async Task<ReservationCode> SetReservationCodeEntity(ReservationCodeDto reservationCodeDto, BuildingLockSystem buildingLockSystem, int reservationId, string userName, CodeStatus? codeStatus = null)
        {
            return new ReservationCode()
            {
                Id = reservationCodeDto.Id,
                ReservationId = reservationId,
                LockCode = reservationCodeDto.LockCode,
                Status = codeStatus ?? CodeStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userName,
                SlotNo = buildingLockSystem == BuildingLockSystem.Arches ? await GetSlotNumber(reservationId) : default,
            };
        }

        private IQueryable<ReservationCode> GetIQueryableReservationCode(Expression<Func<ReservationCode, bool>> predicate = null)
        {
            try
            {
                return _reservationCodeRepo.List(predicate);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task UpdateStatusToPendingAsync(int id)
        {
            try
            {
                var reservationCode = await GetByIdAsync(id);
                reservationCode.Status = CodeStatus.Pending;
                _reservationCodeRepo.Update(reservationCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private int? GetBuildingUnitId(int reservationId)
        {
            // get building unit id against reservation id
            return _stayAccessDbContext.Reservation.FirstOrDefault(x => x.Id == reservationId)?.BuildingUnitId;
        }

        #endregion
    }
}