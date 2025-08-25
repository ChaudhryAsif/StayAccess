using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StayAccess.Tools.Repositories
{
    public class LogService : ILogService
    {

        private readonly IConfiguration _configuration;

        public LogService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public void LogMessage(string logMessage, int? reservationId, bool isCronJob, DateTime estTime, LogType logType, string stackTrace = "") //LogType logType = LogType.Information)
        {
            try
            {
                string source = isCronJob ? $"Automate at {estTime} - " : string.Empty;
                var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
                optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
                using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
                {
                    // add logger info
                    DAL.DomainEntities.Logger entity = new DAL.DomainEntities.Logger
                    {
                        CreatedDate = DateTime.UtcNow,
                        LogTypeId = logType,
                        Message = source + logMessage,
                        ReservationId = reservationId == 0 ? null : reservationId,
                        StackTrace = string.IsNullOrWhiteSpace(stackTrace) ? null : stackTrace,
                    };
                    dbContext.Logger.Add(entity);
                    // save changes to database
                    dbContext.SaveChanges();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task LogsCleanUp()
        {
            // delete logs old than 90 days
            DateTime logsEndDate = DateTime.Now.AddMonths(-3);
            var optionsBuilder = new DbContextOptionsBuilder<StayAccessDbContext>();
            optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);

            using (var dbContext = new StayAccessDbContext(optionsBuilder.Options))
            {
                await DeleteLogsInChunksAsync(dbContext, logsEndDate);
            }
        }

        private async Task DeleteLogsInChunksAsync(StayAccessDbContext dbContext, DateTime logsEndDate)
        {
            LogMessage($"Fetching loggers info", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);
            var deletedLoggers = await dbContext.Logger
                                                .Where(x => x.CreatedDate < logsEndDate)
                                                .Take(100)
                                                .ToListAsync();

            if (deletedLoggers.Any())
            {
                dbContext.Logger.RemoveRange(deletedLoggers);
                await dbContext.SaveChangesAsync();

                LogMessage($"Logger deleted count: {deletedLoggers.Count()}", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);

                // recursively call to process the next chunk
                await DeleteLogsInChunksAsync(dbContext, logsEndDate);
            }
            else
            {
                LogMessage($"No record found to delete", null, true, Utilities.GetCurrentTimeInEST(), LogType.Information);
            }
        }
    }
}
