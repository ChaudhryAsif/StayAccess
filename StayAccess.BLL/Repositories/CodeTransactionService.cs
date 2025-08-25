using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StayAccess.Arches.Interfaces;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.HomeAssistant;
using StayAccess.Latch.Interfaces;
using StayAccess.MC;
using StayAccess.MC.Repositories;
using StayAccess.Tools;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using StayAccess.Tools.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace StayAccess.BLL.Repositories
{
    public class CodeTransactionService : Interfaces.ICodeTransactionService
    {
        private readonly StayAccessDbContext _stayAccessDbContext;
        private readonly IGenericService<CodeTransaction> _codeTransactionRepo;
        private readonly IHomeAssistantService _homeAssistantService;
        private readonly ILatchService _latchService;
        private readonly IBuildingLockSystemService _buildingLockSystemRepo;
        private readonly ILogService _logService;
        private static int _millisecond;

        //not sure if could be private and read only
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public CodeTransactionService(IServiceScopeFactory serviceScopeFactory,
            IGenericService<CodeTransaction> codeTransactionRepo,
            StayAccessDbContext stayAccessDbContext, IHomeAssistantService homeAssistantService,
            ILatchService latchService, ILogService logService, IBuildingLockSystemService buildingLockSystemRepo)
        {
            _codeTransactionRepo = codeTransactionRepo;
            _stayAccessDbContext = stayAccessDbContext;
            _homeAssistantService = homeAssistantService;
            _latchService = latchService;
            _buildingLockSystemRepo = buildingLockSystemRepo;
            _logService = logService;
            _millisecond = 0;
            _serviceScopeFactory = serviceScopeFactory;
        }


        #region public methods

        /// <summary>
        /// start executing pending transactions in background
        /// </summary>
        /// <param name="isCronJob"></param>
        public void ExecuteTransactions(bool isCronJob = false)
        {
            DateTime currentEstTime = Utilities.GetCurrentTimeInEST();
            var codeTransactions = _codeTransactionRepo.List(x => (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry) && x.ExecutionTime <= currentEstTime && x.FailedRetry <= 3
                                                            && ((x.Reservation.BuildingUnit.Building.BuildingLockSystem == BuildingLockSystem.Arches
                                                            && currentEstTime.Date >= (x.Reservation.EarlyCheckIn != null ? x.Reservation.EarlyCheckIn : x.Reservation.StartDate).Value.Date
                                                            && currentEstTime.Date <= (x.Reservation.LateCheckOut != null ? x.Reservation.LateCheckOut : x.Reservation.EndDate).Value.Date) 
                                                            || x.Reservation.BuildingUnit.Building.BuildingLockSystem == BuildingLockSystem.Latch))
                                                            .OrderBy(x => x.CreatedDate).ToList();

            if (codeTransactions.HasAny())
            {
                var reservationIds = codeTransactions.Select(x => x.ReservationId).Distinct().ToList();
                var reservationCodeIds = codeTransactions.Select(x => x.ReservationCodeId).Distinct().ToList();

                var reservations = _stayAccessDbContext.Reservation.Where(x => reservationIds.Contains(x.Id)).ToList();
                var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => reservationCodeIds.Contains(x.Id)).ToList();

                foreach (var codeTransaction in codeTransactions)
                {
                    var reservation = reservations.FirstOrDefault(x => x.Id == codeTransaction.ReservationId);

                    codeTransaction.ModifiedDate = DateTime.UtcNow;
                    codeTransaction.Status = TransactionStatus.InProcess;
                    _codeTransactionRepo.UpdateWithSave(codeTransaction);

                    var reservationCode = reservationCodes.FirstOrDefault(x => x.Id == codeTransaction.ReservationCodeId);

                    if (isCronJob)
                    {
                        Thread.Sleep(30000);
                        ExecuteTransaction(codeTransaction, reservation, reservationCode, currentEstTime, _stayAccessDbContext,
                            _codeTransactionRepo, _homeAssistantService, _latchService);
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                IGenericService<CodeTransaction> _codeTransactionService = scope.ServiceProvider.GetService<IGenericService<CodeTransaction>>();
                                IHomeAssistantService _homeAssistantService = scope.ServiceProvider.GetService<IHomeAssistantService>();
                                ILatchService _latchService = scope.ServiceProvider.GetService<ILatchService>();
                                StayAccessDbContext _stayAccessDbContext = scope.ServiceProvider.GetService<StayAccessDbContext>();
                                _millisecond += 30000;
                                Thread.Sleep(_millisecond);
                                ExecuteTransaction(codeTransaction, reservation, reservationCode, currentEstTime, _stayAccessDbContext,
                                    _codeTransactionService, _homeAssistantService, _latchService);

                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// create transactions for newly generated pending codes. BuildingLockSystem(s): Arches, Latch.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        /// <param name="reservationCodeId"></param>
        public void CreatePendingCodeTransactionsForNew(Reservation reservation, string userName, int reservationCodeId = 0)
        {
            try
            {

                List<int> reservationCodeIds = new List<int>();
                BuildingLockSystem reservationBuildingLockSystem = _buildingLockSystemRepo.GetBuildingLockSystem(reservation).Result;

                if (reservationCodeId > 0)
                {
                    reservationCodeIds.Add(reservationCodeId);
                }
                else
                {
                    reservationCodeIds = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id && x.Status == CodeStatus.Pending)
                                                                     .Select(x => x.Id).ToList();
                }

                if (reservationCodeIds.HasAny())
                {
                    switch (reservationBuildingLockSystem)
                    {
                        case BuildingLockSystem.Arches:
                            foreach (int codeId in reservationCodeIds)
                            {
                                // create new "create" action transactions for front and unit
                                AddTransaction(TransactionAction.Create, DoorType.ArchesFront, TransactionTriggerPoint.MoveIn, reservation, codeId, userName);
                                AddTransaction(TransactionAction.Create, DoorType.ArchesUnit, TransactionTriggerPoint.MoveIn, reservation, codeId, userName);

                                // create new "delete" action transactions for front and unit
                                AddTransaction(TransactionAction.Delete, DoorType.ArchesFront, TransactionTriggerPoint.MoveOut, reservation, codeId, userName);
                                AddTransaction(TransactionAction.Delete, DoorType.ArchesUnit, TransactionTriggerPoint.MoveOut, reservation, codeId, userName);
                            }
                            break;
                        case BuildingLockSystem.Latch:
                            foreach (int codeId in reservationCodeIds)
                            {
                                // create new "create" action transaction record for latch
                                AddTransaction(TransactionAction.Create, DoorType.Latch, TransactionTriggerPoint.Now, reservation, codeId, userName);
                            }
                            break;
                        case BuildingLockSystem.MC:
                            foreach(int codeId in reservationCodeIds)
                            {
                                AddTransaction(TransactionAction.Create, DoorType.MC, TransactionTriggerPoint.Now, reservation, codeId, userName);
                            }
                            break;
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create transactions for pending codes, in case of reservation Move-In date changed. BuildingLockSystem(s): Arches.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        /// <param name="createNew"></param>
        public async Task CreatePendingCodeTransactionsForMIChangeAsync(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, bool createNew = true)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }

            try
            {
                await CreateCodeTransactionSetToStatusDeleted(reservation, userName);

                if (createNew)
                {
                    // fetch pending codes against reservation id
                    var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id && x.Status == DTO.Enums.CodeStatus.Pending).ToList();
                    if (reservationCodes.HasAny())
                    {
                        foreach (var reservationCode in reservationCodes)
                        {
                            // create new "Create" action transactions for front and unit, for each pending code
                            AddTransaction(TransactionAction.Create, DoorType.ArchesFront, TransactionTriggerPoint.MoveIn, reservation, reservationCode.Id, userName);
                            AddTransaction(TransactionAction.Create, DoorType.ArchesUnit, TransactionTriggerPoint.MoveIn, reservation, reservationCode.Id, userName);
                        }

                        // save changes
                        _codeTransactionRepo.Save();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create transactions for pending codes, in case of reservation Move-Out date changed. BuildingLockSystem(s): Arches.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        /// <param name="createNew"></param>
        public async Task CreatePendingCodeTransactionsForMOChange(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, bool createNew = true)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }

            try
            {
                await DeleteCodeTransactionSetToStatusDeleted(reservation, userName);

                if (createNew)
                {
                    // fetch pending codes against reservation id
                    var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id && x.Status == DTO.Enums.CodeStatus.Pending).ToList();
                    if (reservationCodes.HasAny())
                    {
                        foreach (var reservationCode in reservationCodes)
                        {
                            // create new "Delete" action transactions for front and unit, for each pending code
                            AddTransaction(TransactionAction.Delete, DoorType.ArchesFront, TransactionTriggerPoint.MoveOut, reservation, reservationCode.Id, userName);
                            AddTransaction(TransactionAction.Delete, DoorType.ArchesUnit, TransactionTriggerPoint.MoveOut, reservation, reservationCode.Id, userName);
                        }

                    }
                    // save changes
                    _codeTransactionRepo.Save();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task DeleteCodeTransactionSetToStatusDeleted(Reservation reservation, string userName)
        {
            // fetch active transactions against reservation id and "Delete" action
            var codeTransactions = await _codeTransactionRepo.List(x => x.ReservationId == reservation.Id && x.Action == TransactionAction.Delete && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry)).ToListAsync();
            if (codeTransactions.HasAny())
            {
                foreach (var codeTransaction in codeTransactions)
                {
                    // mark existing "Delete" action transactions as deleted as reservation Move-Out date has changed
                    codeTransaction.Status = TransactionStatus.Deleted;
                    codeTransaction.ModifiedBy = userName;
                    codeTransaction.ModifiedDate = DateTime.UtcNow;
                }

                // save changes
                _codeTransactionRepo.Save();
            }
        }

        /// <summary>
        /// create transactions for pending codes, in case of Arches reservation "Unit" changed. BuildingLockSystem(s): Arches.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        public async Task CreatePendingCodeTransactionsForUnitChangeAsync(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }

            try
            {
                // delete existing "Create" action transactions and add new "Create" action transactions 
                await CreatePendingCodeTransactionsForMIChangeAsync(reservation, userName, reservationBuildingLockSystem);

                // delete existing "Delete" action transactions and add new "Delete" action transactions
                await CreatePendingCodeTransactionsForMOChange(reservation, userName, reservationBuildingLockSystem);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create transactions for pending codes, in case of reservation status is changed as "In-Active". BuildingLockSystem(s): Arches, Latch.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        public async Task CreatePendingCodeTransactionsForInActiveAsync(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches && reservationBuildingLockSystem != BuildingLockSystem.Latch && reservationBuildingLockSystem !=BuildingLockSystem.MC)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }
            try
            {
                switch (reservationBuildingLockSystem)
                {
                    case BuildingLockSystem.Arches:
                        // delete existing "Create" action transactions
                        await CreatePendingCodeTransactionsForMIChangeAsync(reservation, userName, reservationBuildingLockSystem, false);

                        // delete existing "Delete" action transactions
                        await CreatePendingCodeTransactionsForMOChange(reservation, userName, reservationBuildingLockSystem, false);
                        break;
                    case BuildingLockSystem.Latch:
                    case BuildingLockSystem.MC:
                        await CreateCodeTransactionSetToStatusDeleted(reservation, userName);
                        break;                   
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// set create code transaction as deleted status. BuildingLockSystem(s): Latch.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        /// <param name="reservationBuildingLockSystem"></param>
        private async Task CreateCodeTransactionSetToStatusDeleted(Reservation reservation, string userName)
        {
            try
            {
                // fetch active transactions against reservation id and "Create" action
                var codeTransactions = await _codeTransactionRepo.List(x => x.ReservationId == reservation.Id && x.Action == TransactionAction.Create && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry)).ToListAsync();
                if (codeTransactions.HasAny())
                {
                    foreach (var codeTransaction in codeTransactions)
                    {
                        // mark existing "Create" action transactions as deleted as reservation Move-In date has changed
                        codeTransaction.Status = TransactionStatus.Deleted;
                        codeTransaction.ModifiedBy = userName;
                        codeTransaction.ModifiedDate = DateTime.UtcNow;
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create transactions for active codes, in case of reservation Move-In/Move-Out date changed. BuildingLockSystem(s): Arches.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        public void CreateActiveCodeTransactionsForMIMOChange(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, bool reservationMIDateInTheFuture = false, bool hasMOChanged = true, bool hasMIChanged = true)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }

            try
            {
                // fetch active transactions against reservation id and "Delete" action
                var codeTransactions = _codeTransactionRepo.List(x => x.ReservationId == reservation.Id &&
                x.Action == TransactionAction.Delete && (x.TriggerPoint != TransactionTriggerPoint.MoveOut || hasMOChanged == true)
                && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry)).ToList();
                _logService.LogMessage($"CreateActiveCodeTransactionsForMIMOChange codeTransactions.HasAny() = {codeTransactions.HasAny()}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information, null);
                if (codeTransactions.HasAny())
                {
                    foreach (var codeTransaction in codeTransactions)
                    {
                        // mark existing "Delete" action transactions as deleted as reservation Move-In/Move-Out date has changed
                        codeTransaction.Status = TransactionStatus.Deleted;
                        codeTransaction.ModifiedBy = userName;
                        codeTransaction.ModifiedDate = DateTime.UtcNow;
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }


                var createUnitCodeTransactions = _codeTransactionRepo.List(x => x.ReservationId == reservation.Id && x.Action == TransactionAction.Create
                                                    && x.DoorType == DoorType.ArchesUnit
                                                    && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry)).ToList();
                _logService.LogMessage($"CreateActiveCodeTransactionsForMIMOChange createUnitCodeTransactions.HasAny() = {createUnitCodeTransactions.HasAny()}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information, null);

                if (createUnitCodeTransactions.HasAny())
                {
                    foreach (var createUnitCodeTransaction in createUnitCodeTransactions)
                    {
                        // mark existing "Delete" action transactions as deleted as reservation Move-In/Move-Out date has changed
                        createUnitCodeTransaction.Status = TransactionStatus.Deleted;
                        createUnitCodeTransaction.ModifiedBy = userName;
                        createUnitCodeTransaction.ModifiedDate = DateTime.UtcNow;
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }


                //original -- wasn't adding code transactions for change of date (when reservation was deleted once before and wasn't set to code status of active)
                // fetch active codes against reservation id
                //var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id).ToList();
                //bool reservationCancelled = _stayAccessDbContext.Reservation.Where(x => x.Id == reservation.Id).Select(x => x.Cancelled).FirstOrDefault();

                //_logService.LogMessage($"CreateActiveCodeTransactionsForMIMOChange reservationCodes.HasAny() = {reservationCodes.HasAny()}. reservationCancelled: {reservationCancelled}.",
                //    reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Warning, null);

                //if (reservationCodes.HasAny() && !reservationCancelled)
                //{
                ///

                // fetch active codes against reservation id
                var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id).ToList();
                bool reservationCancelled = _stayAccessDbContext.Reservation.Where(x => x.Id == reservation.Id).Select(x => x.Cancelled).FirstOrDefault();

                _logService.LogMessage($"CreateActiveCodeTransactionsForMIMOChange reservationCodes.HasAny() = {reservationCodes.HasAny()}. reservationCancelled: {reservationCancelled}.",
                    reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Warning, null);

                if (reservationCodes.HasAny() && !reservationCancelled)
                {

                    foreach (var reservationCode in reservationCodes)
                    {
                        if (hasMOChanged)
                        {
                            // create new "Delete" action transactions for front and unit, for each active code with "MoveOut" TriggerPoint
                            //AddTransaction(TransactionAction.Delete, DoorType.ArchesFront, TransactionTriggerPoint.MoveOut, reservation, reservationCode.Id, userName);
                            AddTransaction(TransactionAction.Update, DoorType.ArchesFront, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName);
                            AddTransaction(TransactionAction.Delete, DoorType.ArchesUnit, TransactionTriggerPoint.MoveOut, reservation, reservationCode.Id, userName);
                        }

                        //if (hasMIChanged)
                        //{
                        //    // create new "Update" action transactions for front, for each active code with "Now" TriggerPoint

                        //    AddTransaction(TransactionAction.Update, DoorType.ArchesFront, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName);



                        //}

                        if (reservationMIDateInTheFuture) //Changed date to later and already started. Already started because the codes are active.
                        {
                            //added to also create the arches unit code -- test if anything breaks because of this
                            AddTransaction(TransactionAction.Create, DoorType.ArchesFront, TransactionTriggerPoint.MoveIn, reservation, reservationCode.Id, userName);
                            // create new "Delete" action transactions if the unit code is active. In case the reservation wasn't supposed to start yet.  -- test if anything breaks because of this
                            AddTransaction(TransactionAction.Delete, DoorType.ArchesFront, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName);

                            //added to also create the arches unit code -- test if anything breaks because of this
                            AddTransaction(TransactionAction.Create, DoorType.ArchesUnit, TransactionTriggerPoint.MoveIn, reservation, reservationCode.Id, userName);

                            // create new "Delete" action transactions if the unit code is active. In case the reservation wasn't supposed to start yet.  -- test if anything breaks because of this
                            AddTransaction(TransactionAction.Delete, DoorType.ArchesUnit, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName);
                        }
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create transactions for active codes, in case of reservation "Unit" changed. BuildingLockSystem(s): Arches.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        public void CreateActiveCodeTransactionsForUnitChange(Reservation reservation, string userName, int oldBuildingUnitId, BuildingLockSystem reservationBuildingLockSystem)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }

            try
            {
                // fetch active transactions against reservation id and "Update/Create/Delete" actions
                var codeTransactions = _codeTransactionRepo.List(x => x.ReservationId == reservation.Id && (x.Action == TransactionAction.Update || x.Action == TransactionAction.Create || x.Action == TransactionAction.Delete)
                                                                   && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry)
                                                                   && !(x.DoorType == DoorType.ArchesFront && x.Action == TransactionAction.Delete && x.TriggerPoint == TransactionTriggerPoint.MoveOut)).ToList();

                _logService.LogMessage($"CreateActiveCodeTransactionsForUnitChange " +
                    $"codeTransactions found to delete IDs: {codeTransactions?.Select(x => x.Id).ToList().ToJsonString()}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information);
                if (codeTransactions.HasAny())
                {
                    foreach (var codeTransaction in codeTransactions)
                    {
                        // mark existing "Delete" action transactions as deleted as reservation Move-In/Move-Out date has changed
                        codeTransaction.Status = TransactionStatus.Deleted;
                        codeTransaction.ModifiedBy = userName;
                        codeTransaction.ModifiedDate = DateTime.UtcNow;
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }

                // fetch active codes against reservation id
                var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id && x.Status == DTO.Enums.CodeStatus.Active).ToList();
                if (reservationCodes.HasAny())
                {
                    foreach (var reservationCode in reservationCodes)
                    {
                        AddTransaction(TransactionAction.Update, DoorType.ArchesFront, TransactionTriggerPoint.MoveIn, reservation, reservationCode.Id, userName);
                        AddTransaction(TransactionAction.Create, DoorType.ArchesUnit, TransactionTriggerPoint.MoveIn, reservation, reservationCode.Id, userName);
                        AddTransaction(TransactionAction.Delete, DoorType.ArchesOldUnit, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName, TransactionStatus.Pending, oldBuildingUnitId);
                        AddTransaction(TransactionAction.Delete, DoorType.ArchesUnit, TransactionTriggerPoint.MoveOut, reservation, reservationCode.Id, userName);
                    }
                    // save changes
                    _codeTransactionRepo.Save();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create transactions for active codes, in case of reservation status is changed as "In-Active". BuildingLockSystem(s): Arches, Latch.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        public void CreateDeleteCodeTransactionsForInActive(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, int oldBuildingUnitId = 0)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches && reservationBuildingLockSystem != BuildingLockSystem.Latch && reservationBuildingLockSystem != BuildingLockSystem.MC)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }

            try
            {
                // fetch active transactions against reservation id and "Delete" action
                //not sure if in process codes should be part of codeTransactionsToSetToStatusDeleted

                List<CodeTransaction> codeTransactionsToSetToStatusDeleted = new();
                switch (reservationBuildingLockSystem)
                {
                    case BuildingLockSystem.Arches:
                        codeTransactionsToSetToStatusDeleted = _codeTransactionRepo.List(x => x.ReservationId == reservation.Id && x.Action == TransactionAction.Delete && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.InProcess || x.Status == TransactionStatus.FailedRetry)).ToList();
                        break;
                    case BuildingLockSystem.Latch:

                        int secondsWhileLoopRan = 0;
                        int timeLimitInSeconds = 600;
                        while (_codeTransactionRepo.List(x => x.ReservationId == reservation.Id && x.Action == TransactionAction.Create && x.Status == TransactionStatus.InProcess).Any())
                        {
                            if (secondsWhileLoopRan == 0)
                            {
                                _logService.LogMessage($"When attempting to delete latch reservation: Waiting for all 'InProcess' CodeTransaction records of action 'Create' for this reservation to stop running." +
                                    $" Because to add a 'Delete' codeTransaction for this reservation need to have it's reservation code (which may not be in the ready in the database yet, as it's in process).",
                                    reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information, null);
                            }
                            else if (secondsWhileLoopRan > timeLimitInSeconds) //600 seconds = 10 min
                            {
                                throw new Exception($"When attempting to delete latch reservation: Got stuck on a Status: 'InProcess', Action: 'Create' code transaction (of this reservation). " +
                                    $"While loop waiting for it to change from 'InProcess' has exceeded it's time limit of '{ timeLimitInSeconds }' seconds.");
                            }
                            //wait until all reservation's "CREATE" codeTransactions stop running (none in process), so that can get the reservationCodeId to delete.
                            System.Threading.Thread.Sleep(2000); // wait 2 seconds
                            secondsWhileLoopRan += 2;
                        }
                        if (secondsWhileLoopRan != default)
                        {//it ran
                            _logService.LogMessage($"When attempting to delete latch reservation: (Exited while loop) Finished waiting for all 'InProcess' CodeTransaction records of action 'Create' for this reservation to stop running." +
                                $" It waited around '{secondsWhileLoopRan}' seconds until it didn't find any 'InProcess' 'Create' code transactions for this reservation." +
                                $" Continuing now with deleting the reservation.",
                                reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information, null);
                        }


                        codeTransactionsToSetToStatusDeleted = _codeTransactionRepo.List(x => x.ReservationId == reservation.Id && (x.Action == TransactionAction.Create || x.Action == TransactionAction.Update || x.Action == TransactionAction.Delete) && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry)).ToList();
                        break;
                }


                if (codeTransactionsToSetToStatusDeleted.HasAny())
                {
                    foreach (var codeTransaction in codeTransactionsToSetToStatusDeleted)
                    {
                        // mark existing "Delete" action transactions as deleted as reservation Move-In/Move-Out date has changed
                        codeTransaction.Status = TransactionStatus.Deleted;
                        codeTransaction.ModifiedBy = userName;
                        codeTransaction.ModifiedDate = DateTime.UtcNow;
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }

                // fetch active codes against reservation id


                List<ReservationCode> reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id && x.Status == DTO.Enums.CodeStatus.Active).ToList();

                if (reservationCodes.HasAny())
                {
                    switch (reservationBuildingLockSystem)
                    {
                        case BuildingLockSystem.Arches:

                            foreach (var reservationCode in reservationCodes)
                            {
                                // create new "Delete" action transactions for front and unit, for each active code
                                AddTransaction(TransactionAction.Delete, DoorType.ArchesFront, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName);
                                AddTransaction(TransactionAction.Delete, DoorType.ArchesUnit, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName, TransactionStatus.Pending, oldBuildingUnitId);
                            }
                            break;
                        case BuildingLockSystem.Latch:
                            foreach (var reservationCode in reservationCodes)
                            {
                                // create new "Delete" action transactions for latch, for each active code
                                AddTransaction(TransactionAction.Delete, DoorType.Latch, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName, TransactionStatus.Pending);
                            }
                            break;
                        //case BuildingLockSystem.MC:
                        //    foreach (var reservationCode in reservationCodes)
                        //    {
                        //        AddTransaction(TransactionAction.Delete, DoorType.MC, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName);
                        //    }
                        //    break;
                    }
                    // save changes
                    _codeTransactionRepo.Save();

                    // if (reservationBuildingLockSystem == BuildingLockSystem.Latch) { ExecuteTransactions(); }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create old unit delete transaction. BuildingLockSystem(s): Arches.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        /// <param name="oldBuildingUnitId"></param>
        public void CreateOldUnitDeleteTransaction(Reservation reservation, string userName, int oldBuildingUnitId, BuildingLockSystem reservationBuildingLockSystem)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Arches)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }

            try
            {

                // fetch active codes against reservation id
                var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id && x.Status == DTO.Enums.CodeStatus.Active).ToList();

                _logService.LogMessage($"CreateOldUnitDeleteTransaction reservationCodes.HasAny() = {reservationCodes.HasAny()}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Information, null);
                if (reservationCodes.HasAny())
                {
                    foreach (var reservationCode in reservationCodes)
                    {
                        AddTransaction(TransactionAction.Delete, DoorType.ArchesOldUnit, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName, TransactionStatus.Pending, oldBuildingUnitId);
                    }
                }
                // save changes
                _codeTransactionRepo.Save();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// create pending transactions for reservation update(s). BuildingLockSystem(s): Latch.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="userName"></param>
        public void CreatePendingCodeTransactionsForChange(Reservation reservation, string userName, BuildingLockSystem reservationBuildingLockSystem, int? oldUnitId)
        {
            if (reservationBuildingLockSystem != BuildingLockSystem.Latch)
            {
                throw new ArgumentException($"Invalid BuildingLockSystem");
            }
            try
            {
                // fetch active transactions against reservation id and "Update" action
                var codeTransactions = _codeTransactionRepo.List(x => x.ReservationId == reservation.Id && x.Action == TransactionAction.Update && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.FailedRetry)).ToList();
                if (codeTransactions.HasAny())
                {
                    foreach (var codeTransaction in codeTransactions)
                    {
                        // mark existing "Delete" action transactions as deleted as reservation Move-Out date has changed
                        codeTransaction.Status = TransactionStatus.Deleted;
                        codeTransaction.ModifiedBy = userName;
                        codeTransaction.ModifiedDate = DateTime.UtcNow;
                    }

                    // save changes
                    _codeTransactionRepo.Save();
                }

                // fetch pending codes against reservation id
                var reservationCodes = _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservation.Id).ToList();
                if (reservationCodes.HasAny())
                {
                    foreach (var reservationCode in reservationCodes)
                    {
                        // create new "Update" action transactions for latch, for each pending code
                        AddTransaction(TransactionAction.Update, DoorType.Latch, TransactionTriggerPoint.Now, reservation, reservationCode.Id, userName, oldBuildingUnitId: oldUnitId);
                    }
                }
                // save changes
                _codeTransactionRepo.Save();

                // if (reservationBuildingLockSystem == BuildingLockSystem.Latch) { ExecuteTransactions(); }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //private BuildingLockSystem CheckAndGetReservationBuildingLockSystem (Reservation reservation, BuildingLockSystem reservationBuildingLockSystem, BuildingLockSystem buildingLock)
        //{
        //    if (reservationBuildingLockSystem != BuildingLockSystem.Latch)
        //    {
        //        buildingLock = GetBuildingLockSystem(reservation.BuildingUnitId).Result;
        //        throw new ArgumentException($"Invalid BuildingLockSystem");
        //    }

        //    return buildingLock;
        //}

        public bool HasPendingCodes(int reservationId)
        {
            try
            {
                return _stayAccessDbContext.ReservationCode.Any(x => x.ReservationId == reservationId && x.Status == DTO.Enums.CodeStatus.Pending);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool HasActiveCodes(int reservationId)
        {
            try
            {
                return _stayAccessDbContext.ReservationCode.Any(x => x.ReservationId == reservationId && x.Status == DTO.Enums.CodeStatus.Active);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<int> GetPendingCodesIds(int reservationId)
        {
            try
            {
                return _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservationId && x.Status == DTO.Enums.CodeStatus.Pending).Select(x => x.Id).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<int> GetDeletedCodesIds(int reservationId)
        {
            try
            {
                return _stayAccessDbContext.ReservationCode.Where(x => x.ReservationId == reservationId && x.Status == DTO.Enums.CodeStatus.Deleted)
                                                            .Select(x => x.Id).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<int> GetAllCodesIds(int reservationId)
        {
            try
            {
                return _stayAccessDbContext.ReservationCode.Select(x => x.Id).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AddTransaction(TransactionAction action, DoorType doorType, TransactionTriggerPoint triggerPoint, Reservation reservation, int reservationCodeId, string userName, TransactionStatus status = TransactionStatus.Pending, int? oldBuildingUnitId = null)
        {
            try
            {
                DateTime executionTime = Utilities.GetCurrentTimeInEST();

                if (triggerPoint == TransactionTriggerPoint.MoveIn)
                    executionTime = reservation.FromDate();
                else if (triggerPoint == TransactionTriggerPoint.MoveOut)
                    executionTime = reservation.ToDate();

                var buildingUnit = _stayAccessDbContext.BuildingUnit.FirstOrDefault(x => x.Id == reservation.BuildingUnitId);
                BuildingUnit oldBuildingUnit = null;
                if (oldBuildingUnitId != null)
                    oldBuildingUnit = _stayAccessDbContext.BuildingUnit.FirstOrDefault(x => x.Id == oldBuildingUnitId);

                var dbCodeTransaction = _codeTransactionRepo.Get(x => x.ReservationId == reservation.Id && x.ReservationCodeId == reservationCodeId && x.Unit == buildingUnit.UnitId
                                                                   && x.Action == action && x.DoorType == doorType && x.TriggerPoint == triggerPoint && x.ExecutionTime == executionTime
                                                                   && x.Status == status);

                if (dbCodeTransaction is null)
                {
                    CodeTransaction codeTransaction = new CodeTransaction
                    {
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = userName,
                        ReservationId = reservation.Id,
                        ReservationCodeId = reservationCodeId,
                        Unit = buildingUnit?.UnitId,
                        Action = action,
                        DoorType = doorType,
                        TriggerPoint = triggerPoint,
                        ExecutionTime = executionTime,
                        Status = status,
                        OldUnit = oldBuildingUnit?.UnitId,
                    };
                    _codeTransactionRepo.Add(codeTransaction);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateAsync(CodeTransaction codeTransaction, string userName)
        {
            try
            {
                CodeTransaction dbCodeTransaction = await GetByIdAsync(codeTransaction.Id);

                if (codeTransaction is null)
                    throw new Exception("Code Transaction not found.");

                dbCodeTransaction.ModifiedDate = DateTime.UtcNow;
                dbCodeTransaction.ModifiedBy = userName;

                _codeTransactionRepo.UpdateWithSave(codeTransaction);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int codeTransactionId)
        {
            try
            {
                CodeTransaction codeTransaction = await GetByIdAsync(codeTransactionId);

                if (codeTransaction is null)
                    throw new Exception("Code Transaction not found.");

                _codeTransactionRepo.DeleteWithSave(codeTransaction);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<CodeTransaction> GetByIdAsync(int codeTransactionId)
        {
            try
            {
                return await _codeTransactionRepo.GetAsync(x => x.Id == codeTransactionId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataSourceResultDto PagedList(int pageNo, int pageSize, Expression<Func<CodeTransaction, bool>> predicate = null, string sortColumn = "Id", bool isAscending = false)
        {
            try
            {
                IQueryable<CodeTransaction> query = GetIQueryableCodeTransaction(predicate);
                query = isAscending ? query.OrderBy(p => EF.Property<object>(p, sortColumn)) : query.OrderByDescending(p => EF.Property<object>(p, sortColumn));

                DataSourceResultDto dataSourceResult = _codeTransactionRepo.PagedList(query, pageNo, pageSize);
                List<CodeTransaction> codeTransactions = (List<CodeTransaction>)dataSourceResult.Data;

                dataSourceResult.Data = codeTransactions;
                return dataSourceResult;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region private methods

        private void ExecuteTransaction(CodeTransaction codeTransaction, Reservation reservation, ReservationCode reservationCode, DateTime currentEstTime, StayAccessDbContext _stayAccessDbContext,
           IGenericService<CodeTransaction> _codeTransactionRepo, IHomeAssistantService _homeAssistantService, ILatchService _latchService)
        {
            try
            {
                HttpStatusCode apiLockSystemResponseHttpStatusCode = HttpStatusCode.OK;

                switch (codeTransaction.DoorType)
                {
                    case DoorType.ArchesFront:
                        switch (codeTransaction.Action)
                        {
                            case TransactionAction.Create:
                                apiLockSystemResponseHttpStatusCode = _homeAssistantService.CreateCodeForFrontDoorAsync(reservation, reservationCode, codeTransaction.Id, false, currentEstTime).GetAwaiter().GetResult();
                                break;
                            case TransactionAction.Update:
                                apiLockSystemResponseHttpStatusCode = _homeAssistantService.ModifiedCodeForFrontDoorAsync(reservation).GetAwaiter().GetResult();
                                break;
                            case TransactionAction.Delete:
                                apiLockSystemResponseHttpStatusCode = _homeAssistantService.DeleteCodeForFrontDoorAsync(reservation, false, currentEstTime).GetAwaiter().GetResult();
                                break;
                            default:
                                break;
                        }
                        break;
                    case DoorType.ArchesUnit:
                        string action = codeTransaction.Action == TransactionAction.Delete ? "delete" : "create";
                        apiLockSystemResponseHttpStatusCode = _homeAssistantService.CreateDeleteUnitCode(action, codeTransaction.Unit, reservation, reservationCode, codeTransaction.Id, currentEstTime);
                        break;
                    case DoorType.ArchesOldUnit:
                        if (codeTransaction.Action == TransactionAction.Delete)
                        {
                            apiLockSystemResponseHttpStatusCode = _homeAssistantService.CreateDeleteUnitCode("delete", codeTransaction.Unit, reservation, reservationCode, codeTransaction.Id, currentEstTime);
                        }
                        break;
                    case DoorType.Latch:
                        ReservationLatchData reservationLatchData = _stayAccessDbContext.ReservationLatchData.Where(x => x.ReservationId == reservation.Id).FirstOrDefault();
                        
                        switch (codeTransaction.Action)
                        {
                            case TransactionAction.Create:
                                apiLockSystemResponseHttpStatusCode = _latchService.CreateLatchReservationAsync(reservation, reservationLatchData, reservationCode, false, currentEstTime).GetAwaiter().GetResult();
                                break;
                            case TransactionAction.Update:
                                var unitChange = codeTransaction.OldUnit != codeTransaction.Unit;
                                apiLockSystemResponseHttpStatusCode = _latchService.UpdateLatchReservationAsync(reservation, reservationLatchData, codeTransaction.OldUnit, unitChange, false, reservationCode, currentEstTime).GetAwaiter().GetResult();
                                break;
                            case TransactionAction.Delete:

                                apiLockSystemResponseHttpStatusCode = _latchService.RemoveLatchReservationAsync(reservation, reservationLatchData, false, reservationCode, currentEstTime).GetAwaiter().GetResult();
                                break;
                            default:
                                break;
                        }
                        break;
                    //case DoorType.LatchOld:
                    //    if (codeTransaction.Action == TransactionAction.Delete)
                    //    {
                    //        _latchService.CreateRemoveLatchReservationAsync(reservation, reservationCode, currentEstTime);
                    //    }
                    //    break;
                    default:
                        break;
                }

                //set status as "Executed" in database
                codeTransaction.ModifiedDate = DateTime.UtcNow;
                codeTransaction.Status = APIResponseService.IsSuccessCode(apiLockSystemResponseHttpStatusCode) ? TransactionStatus.Executed : TransactionStatus.FailedRetry;
                if (codeTransaction.Status == TransactionStatus.FailedRetry)
                {
                    codeTransaction.FailedRetry += 1;
                }
                _codeTransactionRepo.UpdateWithSave(codeTransaction);
            }
            catch (Exception ex)
            {
                try
                {
                    _logService.LogMessage($"An error occurred in execute transaction. Reservation: {reservation.Id}. CodeTransactionId: {codeTransaction.Id}. Error: {ex}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Error, ex.StackTrace);
                }
                catch
                {

                }

                try
                {
                    codeTransaction.ModifiedDate = DateTime.UtcNow;
                    codeTransaction.Status = TransactionStatus.FailedRetry;
                    codeTransaction.FailedRetry += 1;
                    _codeTransactionRepo.UpdateWithSave(codeTransaction);
                }
                catch (Exception ex_2)
                {
                    _logService.LogMessage($"An error occurred in changing code transaction status, after catching an exception. Reservation: {reservation.Id}. CodeTransactionId: {codeTransaction.Id}. Error: {ex_2}", reservation.Id, false, Utilities.GetCurrentTimeInEST(), LogType.Error, ex.StackTrace);
                    throw;
                }
                throw;
            }
        }

        private IQueryable<CodeTransaction> GetIQueryableCodeTransaction(Expression<Func<CodeTransaction, bool>> predicate = null)
        {
            try
            {
                return _codeTransactionRepo.List(predicate);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        public void SaveCodeTransactionError(ErrorDto error)
        {
            try
            {
                var codeTransaction = _stayAccessDbContext.CodeTransaction.FirstOrDefault(x => x.Id == error.CodeTransactionId);
                if (codeTransaction == null)
                    throw new Exception($"CodeTransactionId {error.CodeTransactionId} does not exist.");
                codeTransaction.Status = error.Error ? TransactionStatus.Failed : TransactionStatus.Completed;
                codeTransaction.ErrorMessage = error.ErrorMessage;
                _stayAccessDbContext.SaveChanges();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
