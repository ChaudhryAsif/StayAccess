using StayAccess.DTO.Request.Latch;
using StayAccess.DTO.Responses.Latch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Latch.Interfaces
{
    public interface ILatchIntegrationService
    {
        Task<LatchReturn<GetDoorCodesResponse>> GetReservationDoorCodesAsync(GetDoorCodesRequest request, int reservationId, bool isCronJob, DateTime currentEstTime);
    }
}
