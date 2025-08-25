using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.Latch.Interfaces;
using System.Threading.Tasks;

namespace Automate.Services
{
    public class FrontDoorService
    {
        private readonly IHomeAssistantService _homeAssistantService;

        public FrontDoorService(IHomeAssistantService homeAssistantService)
        {
            _homeAssistantService = homeAssistantService;
        }
        public async Task DeleteExpiredDoorCodesAsync()
        {
            await _homeAssistantService.DeleteExpiredFrontDoorCodes();
        }
    }
}
