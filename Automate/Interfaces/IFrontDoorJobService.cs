using System.Threading.Tasks;

namespace Automate.Interfaces
{
    public interface IFrontDoorJobService
    {
        Task ExecuteForModifiedUnitCodesAsync();
    }
}
