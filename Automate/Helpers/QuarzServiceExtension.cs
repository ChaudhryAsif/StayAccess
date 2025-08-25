using Automate.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Quartz.Impl;
using Quartz.Spi;

namespace Automate.Helpers
{
    public static class QuarzServiceExtension
    {
        public static void UseQuartz(this IServiceCollection services)
        {
            // register job factory
            services.AddSingleton<IJobFactory, JobFactory>();

            // register jobs
            services.AddSingleton<LockCodeTrackingJob>();
            services.AddSingleton<DeleteExpiredDoorCodesJob>();
            services.AddSingleton<LogsCleanUpJob>();

            services.AddSingleton(provider =>
            {
                var schedulerFactory = new StdSchedulerFactory();
                var scheduler = schedulerFactory.GetScheduler().Result;
                scheduler.JobFactory = provider.GetService<IJobFactory>();
                scheduler.Start();
                return scheduler;
            });
        }
    }
}