using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;

namespace Automate.Helpers
{
    public class JobFactory : IJobFactory
    {
        private readonly IServiceProvider serviceProvider;

        public JobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var type = bundle.JobDetail.JobType;
            return serviceProvider.GetRequiredService(type) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            // job completed here
        }
    }
}