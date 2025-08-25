using System.Threading.Tasks;
namespace Automate.Interfaces
{
    public interface ILockCodeTrackingService
    {
        void ExecuteTransactions();
        //Task ExecuteActiveArchesCodesAsync();
        //Task ExecuteFailedArchesCodesAsync();
        Task ExecuteVerificationArchesCodesAsync();
        Task ExecuteArchesLowBatteryLevelEmailNotificationAsync();
    }
}