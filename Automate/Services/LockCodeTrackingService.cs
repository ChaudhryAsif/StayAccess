using Automate.Interfaces;
using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.Latch.Interfaces;
using System.Threading.Tasks;

namespace Automate.Services
{
    public class LockCodeTrackingService : ILockCodeTrackingService
    {
        private readonly IHomeAssistantService _homeAssistantService;
        private readonly ILatchService _latchService;
        private readonly ICodeTransactionService _codeTransactionService;

        public LockCodeTrackingService(IHomeAssistantService homeAssistantService, ILatchService latchService, ICodeTransactionService codeTransactionService)
        {
            _homeAssistantService = homeAssistantService;
            _latchService = latchService;
            _codeTransactionService = codeTransactionService;
        }

        public void ExecuteTransactions()
        {
            _codeTransactionService.ExecuteTransactions(true);
        }

        /// <summary>
        /// Add/Remove Unit Codes for Active/Expired Reservations
        /// Email Notify For Modified Building Units of Active Reservations
        /// </summary>
        /// <returns></returns>
        //public async Task ExecuteActiveArchesCodesAsync()
        //{
        //    await _homeAssistantService.ExecuteActiveCodesForUnitAsync();
        //}

        /// <summary>
        /// Add/Remove Unit Codes for Active/Expired Reservations
        /// Email Notify For Modified Building Units of Active Reservations
        /// </summary>
        /// <returns></returns>
        //public async Task ExecuteActiveLatchCodesAsync()
        //{
        //    await _latchService.ExecuteActiveCodesForDoorAsync();
        //}

        /// <summary>
        /// Verify Front Door And Unit Codes
        /// send all active codes via email in the form of table at 4:00 PM in EST time
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteVerificationArchesCodesAsync()
        {
            await _homeAssistantService.ExecuteVerificationCodesAsync();
        }

        /// <summary>
        /// send email notification from unitlogs for all unit which battery level is below than 40%
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteArchesLowBatteryLevelEmailNotificationAsync()
        {
            await _homeAssistantService.ExecuteLowBatteryLevelEmailNotificationAsync();
        }
    }
}