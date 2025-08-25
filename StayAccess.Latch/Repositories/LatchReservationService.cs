using Microsoft.Extensions.Options;
using Nancy.Json;
using Newtonsoft.Json;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO.Doors.LatchDoor;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request.Latch;
using StayAccess.DTO.Responses.Latch;
using StayAccess.Latch.Interfaces;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace StayAccess.Latch.Repositories
{
    public class LatchReservationService : ILatchReservationService
    {
        private readonly LatchApi _latchApi;
        private readonly ILoggerService<LatchReservationService> _loggerRepo;
        private readonly ILogService _logRepo;
        private readonly StayAccessDbContext _dbContext;
        private readonly IGenericService<LatchAccessToken> _accessTokenService;
        public LatchReservationService(IOptions<LatchApi> latchApi, ILoggerService<LatchReservationService> loggerRepo, ILogService logRepo, StayAccessDbContext context, IGenericService<LatchAccessToken> accessTokenService)
        {
            _latchApi = latchApi.Value;
            _loggerRepo = loggerRepo;
            _logRepo = logRepo;
            _dbContext = context;
            _accessTokenService = accessTokenService;
        }

        public async Task<LatchReturn<CreateReservationResponse>> CreateReservationAsync(CreateReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = $"https://{_latchApi.BaseUrls.Reservation}";
                    string keyIdString = string.Join(", ", request.KeyIds.Select(keyId => $"\"{keyId}\""));
                    JavaScriptSerializer serializer = new();

                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
                    {
                        var jsonRequest = new
                        {
                            firstName = request.FirstName,
                            lastName = request.LastName,
                            email = request.Email,
                            phone = request.Phone,
                            startTime = request.StartTime,
                            endTime = request.EndTime,
                            shareable = request.Shareable,
                            passcodeType = request.PasscodeType,
                            role = request.Role,
                            shouldNotify = request.ShouldNotify,
                            doorUuids = request.KeyIds
                        };
                        requestMessage.Content = new StringContent(JsonConvert.SerializeObject(jsonRequest), Encoding.UTF8, "application/json");
                        try
                        {
                            await Authentication.SignRequestAsync(requestMessage, _latchApi, _logRepo, _dbContext, _accessTokenService);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                        _logRepo.LogMessage($"Creating Latch reservation. Request Body: {requestMessage.Content.ToJsonString()}. Request: {request.ToJsonString()}.", reservationId, isCronJob, currentEstTime, LogType.Information);

                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        _logRepo.LogMessage($"Create Latch reservation. Request Body: {requestMessage.Content.ToJsonString()}. Request: {request.ToJsonString()}. Latch API Response: {response.ToJsonString()}", reservationId, isCronJob, currentEstTime, LogType.Information);
                        string responseText = await response.Content.ReadAsStringAsync();

                        LatchReturn<CreateReservationResponse> returnValue;

                        if (response.IsSuccessStatusCode)
                        {
                            _logRepo.LogMessage($"Successfully created Latch reservation. Request Body: {requestMessage.Content.ToJsonString()}. Request: {request.ToJsonString()}. Response Text: {responseText}", reservationId, isCronJob, currentEstTime, LogType.Information);
                            var responseBody = serializer.Deserialize<CreateReservationResponse>(responseText);
                            returnValue = new LatchReturn<CreateReservationResponse>()
                            {
                                ReturnCode = response.StatusCode,
                                Response = responseBody
                            };
                        }
                        else
                        {
                            _logRepo.LogMessage($"Failed to create Latch reservation. Request Body: {requestMessage.Content.ToJsonString()}. Request: {request.ToJsonString()}. Response: ({(int)response.StatusCode}): {responseText}", reservationId, isCronJob, currentEstTime, LogType.Error);
                            switch (response.StatusCode)
                            {
                                case HttpStatusCode.Forbidden:
                                    returnValue = new LatchReturn<CreateReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = responseText
                                    };
                                    break;
                                default:
                                    var responseBody = serializer.Deserialize<ServerErrorMessage>(responseText);//IF ALL API RETURN VALUES ARE THE SAME => CHANGE TO ServerErrorResponse
                                    returnValue = new LatchReturn<CreateReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = responseBody.message
                                    };
                                    break;
                            }
                        }

                        return returnValue;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<LatchReturn<GetReservationResponse>> GetReservationAsync(GetReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string endpoint = $"/v1/reservations/{request.UserUUid}";
                    string requestUrl = $"https://{_latchApi.BaseUrls.Reservation}{endpoint}";

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl))
                    {
                        await Authentication.SignRequestAsync(requestMessage, _latchApi, _logRepo, _dbContext, _accessTokenService);
                        _logRepo.LogMessage($"Getting Latch Reservation. LatchUserUid {request.UserUUid}.", reservationId, isCronJob, currentEstTime, LogType.Information);
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        _logRepo.LogMessage($"Get Latch Reservation. LatchUserUid {request.UserUUid}. Request: {request.ToJsonString()}. Latch API Response: {response.ToJsonString()}", reservationId, isCronJob, currentEstTime, LogType.Information);
                        string responseText = await response.Content.ReadAsStringAsync();

                        LatchReturn<GetReservationResponse> returnValue;

                        if (response.IsSuccessStatusCode)
                        {
                            _logRepo.LogMessage($"Get Latch Reservation Success. LatchUserUid {request.UserUUid}. Request: {request.ToJsonString()}. Response Text: {responseText}", reservationId, isCronJob, currentEstTime, LogType.Information);
                            var responseBody = JsonConvert.DeserializeObject<GetReservationResponse>(responseText);
                            returnValue = new LatchReturn<GetReservationResponse>()
                            {
                                ReturnCode = response.StatusCode,
                                Response = responseBody
                            };
                        }
                        else
                        {
                            _logRepo.LogMessage($"Get Latch Reservation Failed. LatchUserUid {request.UserUUid}. Request: {request.ToJsonString()}. Response: ({(int)response.StatusCode}): {responseText}", reservationId, isCronJob, currentEstTime, LogType.Error);
                            switch (response.StatusCode)
                            {
                                case System.Net.HttpStatusCode.Forbidden:
                                    returnValue = new LatchReturn<GetReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = responseText
                                    };
                                    break;
                                default:
                                    var errorResponseBody = serializer.Deserialize<ServerErrorResponse>(responseText);
                                    returnValue = new LatchReturn<GetReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = $"error: {errorResponseBody.payload.message.error} - message: {errorResponseBody.payload.message.message}"
                                    };
                                    break;
                            }
                        }

                        return returnValue;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<LatchReturn<EditCancelReservationResponse>> EditDuringReservationAsync(EditDuringReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string endpoint = $"/v1/reservations";
                    string requestUrl = $"https://{_latchApi.BaseUrls.Reservation}{endpoint}";

                    string keyIdString = string.Join(", ", request.KeyIds.Select(keyId => $"\"{keyId}\""));

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl))
                    {
                        string requestBody = @"
                    {
                        ""endTime"": " + request.EndTime.ToUnixTimeSeconds() + @",
                        ""keyIds"": [" + keyIdString + @"]
                    }";
                        requestMessage.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                        await Authentication.SignRequestAsync(requestMessage, _latchApi, _logRepo, _dbContext, _accessTokenService);
                        _logRepo.LogMessage($"Editing Latch reservation in latch API during reservation. Request body: {requestBody.ToJsonString()}. Request: {request.ToJsonString()}.", reservationId, isCronJob, currentEstTime, LogType.Information);
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        _logRepo.LogMessage($"Edit Latch reservation in latch API during reservation. Request body: {requestBody.ToJsonString()}. Request: {request.ToJsonString()}. Latch API Response: {response.ToJsonString()}", reservationId, isCronJob, currentEstTime, LogType.Information);
                        string responseText = await response.Content.ReadAsStringAsync();

                        LatchReturn<EditCancelReservationResponse> returnValue;

                        if (response.IsSuccessStatusCode)
                        {
                            _logRepo.LogMessage($"Successfully edited Latch reservation during reservation. Request body: {requestBody.ToJsonString()}. Request: {request.ToJsonString()}. Response: {responseText}", reservationId, isCronJob, currentEstTime, LogType.Information);
                            returnValue = new LatchReturn<EditCancelReservationResponse>()
                            {
                                ReturnCode = response.StatusCode
                            };
                        }
                        else
                        {
                            _logRepo.LogMessage($"Failed to edit Latch reservation during reservation. Request body: {requestBody.ToJsonString()}. Request: {request.ToJsonString()}. Response: ({(int)response.StatusCode}): {responseText}. ", reservationId, isCronJob, currentEstTime, LogType.Error);
                            switch (response.StatusCode)
                            {
                                case System.Net.HttpStatusCode.Forbidden:
                                case System.Net.HttpStatusCode.GatewayTimeout:
                                    returnValue = new LatchReturn<EditCancelReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = responseText
                                    };
                                    break;
                                default:
                                    try
                                    {
                                        var errorResponseBody = serializer.Deserialize<ServerErrorResponse>(responseText);
                                        returnValue = new LatchReturn<EditCancelReservationResponse>()
                                        {
                                            ReturnCode = response.StatusCode,
                                            ReturnMessage = $"error: {errorResponseBody.payload.message.error} - message: {errorResponseBody.payload.message.message}"
                                        };
                                    }
                                    catch (Exception ex)
                                    {
                                        _logRepo.LogMessage(
                                                $" Trying to deserialize responseText to string (class ServerErrorResponse)," +
                                                $" because wasn't able to deserialize responseText to type object (class InvalidResponse). " +
                                                $" ResponseText: {responseText}" +
                                                $" The deserialize to object Exception: {ex.ToJsonString()}",
                                                reservationId, isCronJob, currentEstTime, LogType.Information);

                                        var errorResponseBody = serializer.Deserialize<InvalidResponse>(responseText);
                                        returnValue = new LatchReturn<EditCancelReservationResponse>()
                                        {
                                            ReturnCode = response.StatusCode,
                                            ReturnMessage = $"error: {errorResponseBody.payload.message}"
                                        };
                                    }
                                    break;
                            }
                        }

                        return returnValue;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<LatchReturn<EditCancelReservationResponse>> EditReservationAsync(UpdateReservationRequest request, int reservationId, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string endpoint = $"/{request.UserUUid}/doors/{request.DoorUUid}";
                    string requestUrl = $"{_latchApi.LatchEndpoints.Delete}{endpoint}";

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Patch, requestUrl))
                    {
                        var jsonRequest = new
                        {
                            shareable = request.Shareable,
                            endTime = request.EndTime
                        };
                        requestMessage.Content = new StringContent(JsonConvert.SerializeObject(jsonRequest), Encoding.UTF8, "application/json");
                        await Authentication.SignRequestAsync(requestMessage, _latchApi, _logRepo, _dbContext, _accessTokenService);
                        _logRepo.LogMessage($"Updating Latch Reservation. Request Body: {requestMessage.Content.ToJsonString()}. ", reservationId, isCronJob, currentEstTime, LogType.Information);
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        _logRepo.LogMessage($"Update Latch Reservation. Request Body: {requestMessage.Content.ToJsonString()}. Door: {request.DoorUUid}. Latch API Response: {response.ToJsonString()}", reservationId, isCronJob, currentEstTime, LogType.Information);
                        string responseText = await response.Content.ReadAsStringAsync();

                        LatchReturn<EditCancelReservationResponse> returnValue;
                        if (response.IsSuccessStatusCode)
                        {
                            _logRepo.LogMessage($"Update Latch Reservation Success in latch API. LatchReservationUserUid: {request.UserUUid}. Door: {request.DoorUUid}. Response Text: {responseText}", reservationId, isCronJob, currentEstTime, LogType.Information);
                            returnValue = new LatchReturn<EditCancelReservationResponse>()
                            {
                                ReturnCode = response.StatusCode
                            };
                        }
                        else
                        {
                            _logRepo.LogMessage($"Update Latch Reservation Failed in latch API. LatchReservationUserUid: {request.UserUUid}. Door: {request.DoorUUid}. Response: ({(int)response.StatusCode}): {responseText}", reservationId, isCronJob, currentEstTime, LogType.Error);
                            switch (response.StatusCode)
                            {
                                case HttpStatusCode.Forbidden:
                                case HttpStatusCode.GatewayTimeout:
                                    returnValue = new LatchReturn<EditCancelReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = responseText
                                    };
                                    break;
                                default:
                                    var errorResponseBody = serializer.Deserialize<ServerErrorMessage>(responseText);
                                    returnValue = new LatchReturn<EditCancelReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = $"error: {errorResponseBody?.error} - message: {errorResponseBody?.message}"
                                    };
                                    break;
                            }
                        }

                        return returnValue;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<LatchReturn<EditCancelReservationResponse>> CancelReservationAsync(CancelReservationRequest cancelReservationRequest, int reservationId, bool isCronJob, DateTime currentEstTime)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string endpoint = $"/{cancelReservationRequest.UserUUid}/doors/{cancelReservationRequest.DoorUUid}";
                    string requestUrl = $"{_latchApi.LatchEndpoints.Delete}{endpoint}";

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUrl))
                    {
                        await Authentication.SignRequestAsync(requestMessage, _latchApi, _logRepo, _dbContext, _accessTokenService);
                        _logRepo.LogMessage($"Canceling Latch Reservation. LatchReservationToken: {requestMessage.Headers.Authorization}. ", reservationId, isCronJob, currentEstTime, LogType.Information);
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        _logRepo.LogMessage($"Cancel Latch Reservation. LatchReservationUserUid: {cancelReservationRequest.UserUUid}. Door: {cancelReservationRequest.DoorUUid}. Latch API Response: {response.ToJsonString()}", reservationId, isCronJob, currentEstTime, LogType.Information);
                        string responseText = await response.Content.ReadAsStringAsync();

                        LatchReturn<EditCancelReservationResponse> returnValue;
                        if (response.IsSuccessStatusCode)
                        {
                            _logRepo.LogMessage($"Cancel Latch Reservation Success in latch API. LatchReservationUserUid: {cancelReservationRequest.UserUUid}. Door: {cancelReservationRequest.DoorUUid}. Response Text: {responseText}", reservationId, isCronJob, currentEstTime, LogType.Information);
                            returnValue = new LatchReturn<EditCancelReservationResponse>()
                            {
                                ReturnCode = response.StatusCode
                            };
                        }
                        else
                        {
                            _logRepo.LogMessage($"Cancel Latch Reservation Failed in latch API. LatchReservationUserUid: {cancelReservationRequest.UserUUid}. Door: {cancelReservationRequest.DoorUUid}. Response: ({(int)response.StatusCode}): {responseText}", reservationId, isCronJob, currentEstTime, LogType.Error);
                            switch (response.StatusCode)
                            {
                                case HttpStatusCode.Forbidden:
                                    returnValue = new LatchReturn<EditCancelReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = responseText
                                    };
                                    break;
                                default:
                                    var errorResponseBody = serializer.Deserialize<ServerErrorMessage>(responseText);
                                    returnValue = new LatchReturn<EditCancelReservationResponse>()
                                    {
                                        ReturnCode = response.StatusCode,
                                        ReturnMessage = $"error: {errorResponseBody.error} - message: {errorResponseBody.message}"
                                    };
                                    break;
                            }
                        }

                        return returnValue;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
