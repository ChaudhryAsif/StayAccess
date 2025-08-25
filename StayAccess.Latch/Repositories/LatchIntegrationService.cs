using Microsoft.Extensions.Options;
using Nancy.Json;
using StayAccess.DTO.Doors.LatchDoor;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request.Latch;
using StayAccess.DTO.Responses.Latch;
using StayAccess.Latch.Interfaces;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Latch.Repositories
{
    public class LatchIntegrationService : ILatchIntegrationService
    {

        private readonly LatchApi _latchApi;
        private readonly ILoggerService<LatchReservationService> _loggerRepo;
        private readonly ILogService _logRepo;

        public LatchIntegrationService(IOptions<LatchApi> latchApi, ILoggerService<LatchReservationService> loggerRepo, ILogService logRepo)
        {
            _latchApi = latchApi.Value;
            _loggerRepo = loggerRepo;
            _logRepo = logRepo;
        }

        public async Task<LatchReturn<GetDoorCodesResponse>> GetReservationDoorCodesAsync(GetDoorCodesRequest request, int reservationId, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                using var client = new HttpClient();
                if (string.IsNullOrWhiteSpace(request.LatchReservationToken))
                    throw new Exception("LatchReservationToken wasn't provided when attempting to get latch reservation door codes. ");

                string requestUrl = $"https://{_latchApi.BaseUrls.Reservation}{_latchApi.LatchEndpoints.Auth}/{request.LatchReservationToken}/{_latchApi.LatchEndpoints.Auth}";

                JavaScriptSerializer serializer = new();

                using HttpRequestMessage requestMessage = new(HttpMethod.Post, requestUrl);
                string requestBody = @"{}";

                requestMessage.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                _logRepo.LogMessage($"Getting latch reservation codes in latch API." +
                    $" Request Body: {requestBody.ToJsonString()}.", reservationId, isCronJob, currentEstTime, LogType.Information);

                HttpResponseMessage response = await client.SendAsync(requestMessage);
                string responseText = await response.Content.ReadAsStringAsync();

                _logRepo.LogMessage($"Get Latch reservation codes in latch API." +
                    $" Request Body: {requestBody.ToJsonString()}." +
                    $" ResponseText: {responseText}." +
                    $" Latch API Response: {response.ToJsonString()}", reservationId, isCronJob, currentEstTime, LogType.Information);

                LatchReturn<GetDoorCodesResponse> returnValue = new();

                if (response.IsSuccessStatusCode)
                {
                    _logRepo.LogMessage($"Latch reservation codes response from latch API request." +
                        $" Request Body: {requestBody.ToJsonString()}." +
                        $" Response Text: {responseText}", reservationId, isCronJob, currentEstTime, LogType.Information);
                    var responseBody = serializer.Deserialize<GetDoorCodesResponse>(responseText);
                    returnValue = new LatchReturn<GetDoorCodesResponse>()
                    {
                        ReturnCode = response.StatusCode,
                        Response = responseBody
                    };
                }
                else
                {
                    _logRepo.LogMessage($"Failed to get latch reservation codes in latch API." +
                        $" Request Body: {requestBody.ToJsonString()}." +
                        $" Response: ({(int)response.StatusCode}): {responseText}", reservationId, isCronJob, currentEstTime, LogType.Error);
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.Forbidden:
                            returnValue = new LatchReturn<GetDoorCodesResponse>()
                            {
                                ReturnCode = response.StatusCode,
                                ReturnMessage = responseText
                            };
                            break;
                        default:
                            try
                            {
                                var responseBody = serializer.Deserialize<InvalidResponseObject>(responseText);
                                returnValue = new LatchReturn<GetDoorCodesResponse>()
                                {
                                    ReturnCode = response.StatusCode,
                                    ReturnMessage = responseBody.payload.message.ToJsonString()
                                };
                            }
                            catch (Exception ex)
                            {
                                _logRepo.LogMessage(
                                    $" Trying to deserialize responseText to string (class InvalildResponse)," +
                                    $" because wasn't able to deserialize responseText to type object (class InvalidResponseObject). " +
                                    $" ResponseText: {responseText}" +
                                    $" The deserialize to object Exception: {ex.ToJsonString()}",
                                    reservationId, isCronJob, currentEstTime, LogType.Information);

                                var responseBody = serializer.Deserialize<InvalidResponse>(responseText);
                                returnValue = new LatchReturn<GetDoorCodesResponse>()
                                {
                                    ReturnCode = response.StatusCode,
                                    ReturnMessage = responseBody.payload.message
                                };
                            }
                            break;
                    }
                }

                try
                {
                    _logRepo.LogMessage($"Latch API response deserialized and converted to object. Object (returnValue): {returnValue.ToJsonString()}.",
                        reservationId, isCronJob, currentEstTime, LogType.Information);
                }
                catch { }

                return returnValue;
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"Error occured in getting latch reservation codes in latch API." +
                       $" Exception: {ex.ToJsonString()}", reservationId, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }
    }
}
