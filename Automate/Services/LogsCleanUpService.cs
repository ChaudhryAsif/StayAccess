using Automate.Interfaces;
using StayAccess.Tools.Interfaces;
using System.Threading.Tasks;

namespace Automate.Services
{
    public class LogsCleanUpService : ILogsCleanUpService
    {
        private readonly ILogService _logService;

        public LogsCleanUpService(ILogService logService)
        {
            _logService = logService;
        }

        public async Task LogsCleanUpAsync()
        {
            await _logService.LogsCleanUp();
        }
    }
}
