using StayAccess.DTO.Responses.Latch;
using System.Net;

namespace StayAccess.Latch
{
    public class LatchReturn<TResponse> where TResponse : ILatchResponse, new()
    {
        public HttpStatusCode ReturnCode { get; set; }
        public string? ReturnMessage { get; set; }
        public TResponse Response { get; set; } = new TResponse();
        
    }
}
