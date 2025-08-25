using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Request.MC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.MC.Interfaces
{
    public interface IMCService
    {
        Task<HttpStatusCode> RemoveReservationAsync(Reservation reservation, ReservationMCData reservationMcData);
        Task<HttpStatusCode> CreateReservationAsync(Reservation reservation, string userName);
        Task<HttpStatusCode> UpdateReservationAsync(MCReservationUpdateRequest request, ReservationMCData mcData, int mcId, Reservation reservation);
    }
}
