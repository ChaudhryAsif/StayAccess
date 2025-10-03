using Automate.Jobs;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace Automate
{
    public class QuartzStartup
    {
        private IScheduler scheduler;
        private readonly IConfiguration configuration;

        public QuartzStartup(IScheduler scheduler, IConfiguration configuration)
        {
            this.scheduler = scheduler;
            this.configuration = configuration;
        }
        // testing.
        public void Start()
        {
            ScheduleJob<LockCodeTrackingJob>(LockCodeTrackingJob.JobName, configuration.GetValue<string>("LockCodeTrackingJobTime"));
            ScheduleJob<DeleteExpiredDoorCodesJob>(DeleteExpiredDoorCodesJob.JobName, configuration.GetValue<string>("DeleteExpiredDoorCodesJobTime"));
            ScheduleJob<LogsCleanUpJob>(LogsCleanUpJob.JobName, configuration.GetValue<string>("LogsCleanUpJobTime"));
        }

        public void Stop()
        {
            if (scheduler == null)
            {
                return;
            }

            if (scheduler.Shutdown(true).Wait(30000))
            {
                scheduler = null;
            }
        }

        private void ScheduleJob<T>(string jobName, string schedule) where T : IJob
        {
            var job = JobBuilder.Create<T>().WithIdentity(jobName).Build();
            var jobTrigger = TriggerBuilder.Create()
                .WithIdentity(jobName)
                .StartNow()
                .WithCronSchedule(schedule)
                .Build();

            scheduler.ScheduleJob(job, jobTrigger).Wait();
        }
    }
}