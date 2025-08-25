using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Doors.MCDoor;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request.MC;
using StayAccess.DTO.Responses.MC;
using StayAccess.MC.Interfaces;
using StayAccess.Tools.Interfaces;
using System.Net;
using System.Net.Http.Json;

namespace StayAccess.MC.Repositories
{
    public class MCService : IMCService
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerService<MCService> _loggerService;
        private readonly MCApi _mcApi;
        private readonly StayAccessDbContext _dbContext;
        public MCService(IConfiguration configuration, IOptions<MCApi> mcApi, StayAccessDbContext dbContext, ILoggerService<MCService> loggerService)
        {
            _configuration = configuration;
            _mcApi = mcApi.Value;
            _dbContext = dbContext;
            _loggerService= loggerService;
        }
        public async Task<HttpStatusCode> RemoveReservationAsync(Reservation reservation, ReservationMCData reservationMcData)
        {
            try
            {
                HttpResponseMessage responseMessage = await CancelReservation(reservationMcData);
                //can enter if statements to set the statusCdoe toReturn
                if (responseMessage.IsSuccessStatusCode)
                {
                    try
                    {
                        var response = await responseMessage.Content.ReadAsStringAsync();
                        reservationMcData.ReservationStatus = ReservationStatus.Deleted;
                         _dbContext.SaveChanges();
                        _loggerService.Add(LogType.Information, $"Changed status to deleted for Monte Carlo: {JsonConvert.SerializeObject(reservation)}.", reservation.Id);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
                else
                {
                    _loggerService.Add(LogType.Error, $"Error while attempting to change status to deleted for a Monte Carlo reservation: {JsonConvert.SerializeObject(reservation)}." +
                                        $" Recieved a {responseMessage.StatusCode}", reservation.Id);
                }
                return responseMessage.StatusCode;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private async Task<HttpResponseMessage> CancelReservation(ReservationMCData reservationMcData)
        {
            try
            {
                _loggerService.Add(LogType.Information, $"Attempting to cancel the reservation {reservationMcData}. Passing in the id, {reservationMcData.McId}", reservationMcData.ReservationId);
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = $"{_mcApi.BaseUrl}/reservation/{reservationMcData.McId}";
                    client.DefaultRequestHeaders.Add("api-key", _mcApi.ApiKey);
                    var response = await client.DeleteAsync(requestUrl);
                    _loggerService.Add(LogType.Information, $"Response recieved after attempting to cancel the reservation {reservationMcData} is {response}", reservationMcData.ReservationId);
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<HttpStatusCode> CreateReservationAsync(Reservation reservation, string userName )
        {
            try
            {
                if (!checkIfValidUnit(reservation.BuildingUnit.UnitId))
                    throw new NullReferenceException();
                else
                {
                    HttpResponseMessage responseMessage = await CreateReservation(reservation);
                    var jsonString = await responseMessage.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<MCApiResponse>(jsonString);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        if (responseData != null)
                        {
                            try
                            {

                                ReservationMCData mcData = new ReservationMCData()
                                {
                                    McId = (int)responseData.id,
                                    McIdRoom = (int)responseData.id_room,
                                    FirstName = responseData.fname,
                                    LastName = responseData.lname,
                                    ReservationId = reservation.Id,
                                    CheckIn = DateTime.Parse(responseData.check_in),
                                    CheckOut = DateTime.Parse(responseData.check_out),
                                    UnitId = reservation.BuildingUnit.UnitId,
                                    CreatedDate = DateTime.UtcNow,
                                    CreatedBy = userName,
                                    ReservationStatus = ReservationStatus.Created

                                };
                                _dbContext.ReservationMCData.Add(mcData);
                                _dbContext.SaveChanges(true);
                                _loggerService.Add(LogType.Information, $"Created new reservation: {JsonConvert.SerializeObject(reservation)} for Monte Carlo.", reservation.Id);

                            }

                            catch (Exception)
                            {
                                _loggerService.Add(LogType.Error, $"Error while creating new reservation for Monte Carlo: {JsonConvert.SerializeObject(reservation)}." +
                                                                 $" Recieved a {responseMessage.StatusCode} - {jsonString}", reservation.Id);
                                throw;
                            }
                        }
                    }
                    else
                    {
                        _loggerService.Add(LogType.Error, $"Error while creating new reservation for Monte Carlo: {JsonConvert.SerializeObject(reservation)}." +
                                                                $" Recieved a {responseMessage.StatusCode} - {jsonString}", reservation.Id);
                    }
                    return responseMessage.StatusCode;
                }   

            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<HttpStatusCode> UpdateReservationAsync(MCReservationUpdateRequest request, ReservationMCData mcData, int mcId, Reservation reservation)
        {
            try
            {
                if (!checkIfValidUnit(mcData.UnitId))
                    throw new NullReferenceException();
                else
                {
                    HttpResponseMessage responseMessage = await UpdateReservation(request, mcId, reservation.Id);
                    var jsonString = await responseMessage.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<MCApiResponse>(jsonString);
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        try
                        {
                            mcData.CheckIn = DateTime.Parse(responseData.check_in);//DateTime.Parse(request.check_in);
                            mcData.CheckOut = DateTime.Parse(responseData.check_out);//DateTime.Parse(request.check_out);
                            mcData.FirstName = responseData.fname;//request.fname;
                            mcData.LastName = responseData.lname;//request.lname;
                            mcData.UnitId = request.room_name;
                            mcData.McIdRoom = (int)responseData.id_room;
                            _dbContext.SaveChanges(true);
                            _loggerService.Add(LogType.Information, $"Updated reservation: {JsonConvert.SerializeObject(reservation)} for Monte Carlo.", reservation.Id);
                        }
                        catch (Exception)
                        {
                            _loggerService.Add(LogType.Error, $"Error while updating reservation for Monte Carlo: {JsonConvert.SerializeObject(reservation)}." +
                                                                $" Recieved a {responseMessage.StatusCode} - {jsonString}", reservation.Id);
                            throw;
                        }
                    }
                    else
                    {
                        _loggerService.Add(LogType.Error, $"Error while updating reservation for Monte Carlo: {JsonConvert.SerializeObject(reservation)}." +
                                                               $" Recieved a {responseMessage.StatusCode} - {jsonString}", reservation.Id);
                    }
                    return responseMessage.StatusCode;
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
        private async Task<HttpResponseMessage> UpdateReservation( MCReservationUpdateRequest request, int id, int reservationId)
        {
            try
            {
                _loggerService.Add(LogType.Information, $"Attempting to update the reservation with mcId {id}. Passing in {JsonConvert.SerializeObject(request)} as payload.", reservationId);
                using (HttpClient client = new HttpClient())
                {
                    
                    string requestUrl = _mcApi.BaseUrl + $"/reservation/update/{id}";
                    client.DefaultRequestHeaders.Add("api-key", _mcApi.ApiKey);
                    HttpResponseMessage httpResponse = await client.PutAsJsonAsync(requestUrl, request);
                    var response =await httpResponse.Content.ReadAsStringAsync();
                    _loggerService.Add(LogType.Information, $"Response recieved after attempting to update the reservation with mcId {id} is {response}", reservationId);
                    return httpResponse;
                }
            }
            catch(Exception)
            {
                throw;
            }
        }
        private bool checkIfValidUnit(string unitId)
        {
            var buildingUnit = _dbContext.BuildingUnit.FirstOrDefault(x => x.UnitId == unitId);
            return buildingUnit != null ? true : false;

        }
        private async Task<HttpResponseMessage> CreateReservation(Reservation reservation)
        {
            MCReservationRequest reservationRequest = new MCReservationRequest
            {
                check_out = reservation.EndDate.ToString("yyyy-MM-dd"),
                check_in = reservation.StartDate.ToString("yyyy-MM-dd"),
                fname = reservation.FirstName,
                lname = reservation.LastName,
                room_name = reservation.BuildingUnit.UnitId.ToString()
            };

            try
            {
                _loggerService.Add(LogType.Information, $"Attempting to create the reservation with reservationId {reservation.Id}. Passing in {JsonConvert.SerializeObject(reservationRequest)} as payload.", reservation.Id);
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = _mcApi.BaseUrl + "/reservation/single";
                    client.DefaultRequestHeaders.Add("api-key", _mcApi.ApiKey);
                    HttpResponseMessage httpResponse = await client.PostAsJsonAsync(requestUrl, reservationRequest);
                    var response = await httpResponse.Content.ReadAsStringAsync();
                    _loggerService.Add(LogType.Information, $"Response recieved after attempting to create the reservation with reservationId {reservation.Id} is {response}", reservation.Id);
                    return httpResponse;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

