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
    public class LogsCleanUpJob : IJob
    {
        public const string JobName = "LogsCleanUpJob";
        private readonly IServiceProvider serviceProvider;

        public LogsCleanUpJob(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            ILoggerService<LogsCleanUpJob> loggerService = null;
            ILogsCleanUpService logsCleanUpService = null;

            try
            {
                // create scope
                using var scope = serviceProvider.CreateScope();
                loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService<LogsCleanUpJob>>();
                logsCleanUpService = scope.ServiceProvider.GetRequiredService<ILogsCleanUpService>();

                // start job operation as background task
                loggerService.Add(LogType.Information, $"Starting {JobName}.", null);

                await logsCleanUpService.LogsCleanUpAsync();
            }
            catch (Exception ex)
            {
                string error = $"Error occurred in {JobName}. Error: {ex.Message}.";
                loggerService.Add(LogType.Error, error, null, ex.StackTrace);
            }
        }
    }
}
