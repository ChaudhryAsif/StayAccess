using StayAccess.DAL.DomainEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Interfaces
{
    public interface IAPIProviderService
    {
        Task PostStringToMri(int reservationId, string endpoint, string payload, DateTime currentEstTime, bool isCronJob);
    }
}
