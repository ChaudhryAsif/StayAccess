using CL.Common.Models;
using CL.Common.Services;
using Microsoft.Extensions.Options;
using Polly;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Repositories
{
    public class APIProviderService : IAPIProviderService
    {
        private readonly ILogService _logRepo;
        private readonly APIProviderSettings _apiProviderSettings;

        public APIProviderService(ILogService logRepo, IOptions<APIProviderSettings> apiProviderSettings)
        {
            _logRepo = logRepo;
            _apiProviderSettings = apiProviderSettings.Value;
        }

        public async Task PostStringToMri(int reservationId, string endpoint, string payload, DateTime currentEstTime, bool isCronJob)
        {
            try
            {
                _logRepo.LogMessage($"Posting to MRI. ReservationId: {reservationId}. BaseUrl: {_apiProviderSettings.BaseURL}. Endpoint: {endpoint}. Payload: {payload}.",
                                        reservationId, isCronJob, currentEstTime, LogType.Information);

                Random jitterer = new Random();
                var retryPolicy = Policy
                    .Handle<Exception>()
                    .OrResult<HttpResponseMessage>(r => r.StatusCode != HttpStatusCode.OK)
                    .WaitAndRetryAsync(5, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 100)),
                        (exception, timeSpan, retryCount, context) =>
                        {
                            _logRepo.LogMessage($"Attempt {retryCount} - Failed to post to MRI. ReservationId: {reservationId}. BaseUrl: {_apiProviderSettings.BaseURL}. Endpoint: {endpoint}. Payload: {payload}. Retrying in {timeSpan.TotalSeconds} seconds.", 
                                reservationId, isCronJob, currentEstTime, LogType.Error);
                        });
               
                HttpResponseMessage httpResponse = await retryPolicy.ExecuteAsync(async () =>
                {
                    return await APICall.ApiPostCallString(_apiProviderSettings, endpoint, payload);
                });
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    _logRepo.LogMessage($"Successfully posted to MRI. ReservationId: {reservationId}. BaseUrl: {_apiProviderSettings.BaseURL}. Endpoint: {endpoint}. Payload: {payload}.",
                                       reservationId, isCronJob, currentEstTime, LogType.Information);
                }
                else
                {
                    _logRepo.LogMessage($"Failed to post to MRI - unsuccessful status code. ReservationId: {reservationId}. BaseUrl: {_apiProviderSettings.BaseURL}. Endpoint: {endpoint}. Payload: {payload}. HttpResponseReasonPhrase: {httpResponse.ReasonPhrase}.",
                                     reservationId, isCronJob, currentEstTime, LogType.Error);
                }
            }
            catch (Exception ex)
            {
                _logRepo.LogMessage($"An error occurred in posting to Mri. ReservationId: {reservationId}. BaseUrl: {_apiProviderSettings.BaseURL}. Endpoint: {endpoint}. Payload: {payload}. Ex: {ex}.",
                                     reservationId, isCronJob, currentEstTime, LogType.Error);
                throw;
            }
        }
    }
}
