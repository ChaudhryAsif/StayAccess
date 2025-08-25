using StayAccess.DTO.Enums;
using System;
using System.Threading.Tasks;

namespace StayAccess.Tools.Interfaces
{
    public interface ILogService
    {
        public void LogMessage(string logMessage, int? reservationId, bool isCronJob, DateTime estTime, LogType logType, string stackTrace = ""); // = LogType.Information);
        Task LogsCleanUp();
    }
}
