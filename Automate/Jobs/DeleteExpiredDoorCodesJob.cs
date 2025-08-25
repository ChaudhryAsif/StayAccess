using Automate.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Interfaces;
using System;
using System.Threading.Tasks;

namespace Automate.Jobs
{
    [DisallowConcurrentExecution]
    public class DeleteExpiredDoorCodesJob : IJob
    {
        public const string JobName = "DeleteExpiredDoorCodesJob";
        private readonly IServiceProvider _serviceProvider;
        public DeleteExpiredDoorCodesJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            ILoggerService<DeleteExpiredDoorCodesJob> loggerService = null;
            FrontDoorService frontDoorService = null;

            try
            {
                // create scope
                using var scope = _serviceProvider.CreateScope();
                loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService<DeleteExpiredDoorCodesJob>>();
                frontDoorService = scope.ServiceProvider.GetRequiredService<FrontDoorService>();

                // start job operation as background task
                loggerService.Add(LogType.Information, $"Starting {JobName}.", null);

                await Task.Run(() => frontDoorService.DeleteExpiredDoorCodesAsync());
            }
            catch (Exception ex)
            {
                string error = $"Error occurred in {JobName}. Error: {ex.Message}.";
                loggerService.Add(LogType.Error, error, null, ex.StackTrace);
            }
        }
    }
}
