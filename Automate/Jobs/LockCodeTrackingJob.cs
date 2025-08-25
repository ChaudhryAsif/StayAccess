using Automate.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Interfaces;
using System;
using System.Threading.Tasks;

namespace Automate.Jobs
{
    [DisallowConcurrentExecution]
    public class LockCodeTrackingJob : IJob
    {
        public const string JobName = "LockCodeTrackingJob";
        private readonly IServiceProvider serviceProvider;

        public LockCodeTrackingJob(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            ILockCodeTrackingService trackingService = null;
            ILoggerService<LockCodeTrackingJob> loggerService = null;

            try
            {
                // create scope
                using var scope = serviceProvider.CreateScope();
                trackingService = scope.ServiceProvider.GetRequiredService<ILockCodeTrackingService>();
                loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService<LockCodeTrackingJob>>();

                // start job operation as background task
                loggerService.Add(LogType.Information, $"Starting {JobName}.", null);

                await Task.Run(() => trackingService.ExecuteTransactions());
                //await Task.Run(() => trackingService.ExecuteActiveCodesAsync());
                //await Task.Run(() => trackingService.ExecuteFailedCodesAsync());
                await Task.Run(() => trackingService.ExecuteVerificationArchesCodesAsync());
                await Task.Run(() => trackingService.ExecuteArchesLowBatteryLevelEmailNotificationAsync());
            }
            catch (Exception ex)
            {
                string error = $"Error occurred in {JobName}. Error: {ex.Message}.";
                loggerService.Add(LogType.Error, error, null, ex.StackTrace);
            }
        }
    }
}