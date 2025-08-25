using StayAccess.DTO.Request.Latch;
using StayAccess.DTO.Responses.Latch;
using StayAccess.Latch.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Latch.Interfaces
{
    public interface ILatchReservationService
    {
        //previous
        //   Task<CreateReservationResponse> CreateReservation(string clientId, string secretKey, DateTimeOffset startTime, DateTimeOffset endTime, IEnumerable<Guid> keyIds, string firstName, string lastName, int allowedKeycardCount, DateTime currentEstTime);
        //   Task<GetReservationResponse> GetReservation(string clientId, string secretKey, string reservationToken);

        //updated


        Task<LatchReturn<CreateReservationResponse>> CreateReservationAsync(CreateReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime); 
        Task<LatchReturn<GetReservationResponse>> GetReservationAsync(GetReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime); 
        Task<LatchReturn<EditCancelReservationResponse>> EditReservationAsync(UpdateReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime); 
        Task<LatchReturn<EditCancelReservationResponse>> EditDuringReservationAsync(EditDuringReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime); 
        Task<LatchReturn<EditCancelReservationResponse>> CancelReservationAsync(CancelReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime); 
        // bool IsSuccessCode(HttpStatusCode statusCode);

        //public abstract Task<LatchReturn<CreateReservationResponse>> CreateReservation(CreateReservationRequest request); 
        //public abstract Task<LatchReturn<GetReservationResponse>> GetReservation(GetReservationRequest request);
        //public abstract Task<LatchReturn<EditCancelReservationResponse>> EditReservation(EditReservationRequest request); 
        //public abstract Task<LatchReturn<EditCancelReservationResponse>> CancelReservation(CancelReservationRequest request);
        //public bool IsSuccessCode(HttpStatusCode statusCode);
    }
}
