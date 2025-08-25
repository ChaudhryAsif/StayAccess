using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.HomeAssistant;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface ICodeTransactionService
    {
        void ExecuteTransactions(bool isCronJob = false);

        void CreatePendingCodeTransactionsForNew(Reservation reservation, string userName, int reservationCodeId = 0);
        Task CreatePendingCodeTransactionsForMIChangeAsync(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, bool createNew = true);
        Task CreatePendingCodeTransactionsForMOChange(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, bool createNew = true);
        Task CreatePendingCodeTransactionsForUnitChangeAsync(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem);
        Task CreatePendingCodeTransactionsForInActiveAsync(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem);

        void CreateActiveCodeTransactionsForMIMOChange(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, bool reservationMIDateInTheFuture = false, bool hasMOChanged = true, bool hasMIChanged = true);

        void CreateDeleteCodeTransactionsForInActive(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, int oldBuildingUnitId = 0);
        void CreateActiveCodeTransactionsForUnitChange(Reservation reservation, string userName, int oldBuildingUnitId, BuildingLockSystem reservationBuildingLockSystem);
        void CreateOldUnitDeleteTransaction(Reservation reservation, string userName, int oldBuildingUnitId, BuildingLockSystem reservationBuildingLockSystem);
        void CreatePendingCodeTransactionsForChange(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, int? oldUnitId);

        bool HasPendingCodes(int reservationId);
        bool HasActiveCodes(int reservationId);

        List<int> GetPendingCodesIds(int reservationId);
        List<int> GetDeletedCodesIds(int reservationId);
        List<int> GetAllCodesIds(int reservationId);

        /// <summary>
        /// Update code transaction
        /// </summary>
        /// <param name="codeTransaction"></param>
        /// <returns></returns>
        Task UpdateAsync(CodeTransaction codeTransaction, string userName);

        /// <summary>
        /// Delete code transaction
        /// </summary>
        /// <param name="codeTransactionId"></param>
        /// <returns></returns>
        Task DeleteAsync(int codeTransactionId);

        /// <summary>
        ///  Get by id code transaction
        /// </summary>
        /// <param name="codeTransactionId"></param>
        /// <returns></returns>
        Task<CodeTransaction> GetByIdAsync(int codeTransactionId);

        /// <summary>
        /// Paged list of code transaction
        /// </summary>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <param name="predicate"></param>
        /// <param name="sortColumn"></param>
        /// <param name="isAscending"></param>
        /// <returns></returns>
        DataSourceResultDto PagedList(int pageNo, int pageSize, Expression<Func<CodeTransaction, bool>> predicate = null, string sortColumn = "Id", bool isAscending = false);

        void SaveCodeTransactionError(ErrorDto error);
    }
}
