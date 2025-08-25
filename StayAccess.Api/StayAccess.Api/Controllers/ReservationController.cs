using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.ReservationCode;
using StayAccess.DTO.Reservations;
using StayAccess.DTO.Responses;
using StayAccess.DTO.Responses.Arches;
using StayAccess.DTO.Responses.Logger;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReservationController : BaseController
    {
        private readonly IReservationService _reservationService;
        private readonly ILoggerService<ReservationController> _loggerService;
        private readonly IBuildingLockSystemService _buildingLockSystemRepo;

        public ReservationController(IReservationService reservationService, ILoggerService<ReservationController> loggerService, IBuildingLockSystemService buildingLockSystemRepo)
        {
            _reservationService = reservationService;
            _loggerService = loggerService;
            _buildingLockSystemRepo = buildingLockSystemRepo;
        }

        [HttpPost]
        [Route("Save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaveResponseDto>> SaveAsync(ReservationRequestDto reservationDto)
        {
            try
            {
                SaveResponseDto response = new();

                BuildingLockSystem newUnitBuildingLockSystem;
                if (reservationDto.Id > 0)
                {
                    newUnitBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystemByBuildingUnitId(reservationDto.GetBuildingUnitId());
                }
                else
                {
                    newUnitBuildingLockSystem = await _reservationService.AdjustReservationRequestDto(reservationDto);
                }

                var matchedReservation = await _reservationService.GetMatchedReservationAsync(reservationDto, newUnitBuildingLockSystem);
                if (matchedReservation != null && newUnitBuildingLockSystem != BuildingLockSystem.MC && (newUnitBuildingLockSystem != BuildingLockSystem.Latch || reservationDto.Id == default))
                {
                    response.Id = matchedReservation.Id;
                }
                else
                {
                    if (reservationDto.Id > 0)
                    {
                        ReservationMCData reservationMCData = _reservationService.GetFirstOrDefault(reservationDto.Id);
                        if (reservationMCData?.ReservationStatus == ReservationStatus.Deleted && newUnitBuildingLockSystem == BuildingLockSystem.MC)
                        {
                            ReservationCodeResponseDto mcResponse = await CreateANewReservationForMCAsync(reservationDto);
                            response.Id = mcResponse?.ReservationId ?? default; 
                        }
                        else if (matchedReservation != null && reservationMCData == default && newUnitBuildingLockSystem == BuildingLockSystem.MC)
                        {
                            await RecreateMCReservationAsync(matchedReservation);
                            response.Id = matchedReservation.Id;
                        }
                        else
                        {
                            await _reservationService.UpdateAsync(reservationDto, GetLoggedInUserName, newUnitBuildingLockSystem);
                            response.Id = reservationDto.Id;
                        }
                    }
                    else
                    {
                        switch (newUnitBuildingLockSystem)
                        {
                            case BuildingLockSystem.Arches:
                                var addResponseDto = await _reservationService.AddAsync(reservationDto, GetLoggedInUserName, newUnitBuildingLockSystem, false);
                                if (addResponseDto != null)
                                    response.Id = addResponseDto.ReservationId;
                                break;
                            case BuildingLockSystem.Latch:
                                //Same type of add as in Latch Reservation.AddWithCodes endpoint
                                ReservationCodeResponseDto responseDto = new();
                                ReservationWithCodesDto reservationWithCodesDto = new()
                                {
                                    Id = reservationDto.Id,
                                    BuildingUnitId = reservationDto.BuildingUnitId,
                                    Code = reservationDto.Code,
                                    StartDate = reservationDto.StartDate,
                                    EndDate = reservationDto.EndDate,
                                    EarlyCheckIn = reservationDto.EarlyCheckIn,
                                    LateCheckOut = reservationDto.LateCheckOut,
                                    Cancelled = reservationDto.Cancelled,
                                    NewBuildingUnitId = reservationDto.NewBuildingUnitId,
                                    NewCode = reservationDto.NewCode,
                                    FirstName = reservationDto.FirstName, 
                                    LastName = reservationDto.LastName
                                };

                                ReservationCodeResponseDto reservationCodeResponseDto = new();
                                reservationCodeResponseDto = await _reservationService.AddWithCodes(reservationWithCodesDto, responseDto, false, GetLoggedInUserName);
                                response.Id = reservationCodeResponseDto?.ReservationId ?? default;
                                break;
                            case BuildingLockSystem.MC:
                                ReservationCodeResponseDto mcResponse = new();
                                mcResponse = await _reservationService.AddAsync(reservationDto, GetLoggedInUserName, newUnitBuildingLockSystem, false);
                                response.Id = mcResponse?.ReservationId ?? default;
                                break;

                        }
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in saving Reservation: {JsonConvert.SerializeObject(reservationDto)}. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        private async Task<ReservationCodeResponseDto> CreateANewReservationForMCAsync(ReservationRequestDto reservationDto)
        {
            ReservationRequestDto newRequest = new()
            {
                Id = default,
                BuildingUnitId = reservationDto.BuildingUnitId,
                Code = reservationDto.Code,
                StartDate = reservationDto.StartDate,
                EndDate = reservationDto.EndDate,
                EarlyCheckIn = reservationDto.EarlyCheckIn,
                LateCheckOut = reservationDto.LateCheckOut,
                Cancelled = false,
                FirstName = reservationDto.FirstName,
                LastName = reservationDto.LastName
            };
            ReservationCodeResponseDto mcResponse = new();
            mcResponse = await _reservationService.AddAsync(newRequest, GetLoggedInUserName, BuildingLockSystem.MC, false); 
            return mcResponse;

        }
        private async Task RecreateMCReservationAsync(Reservation matchedReservation)
        {
            //ReservationCodeResponseDto mcResponse = new();
           await _reservationService.AddMCReservationAsync(matchedReservation, GetLoggedInUserName);
            //if(httpStatusCode == HttpStatusCode.OK)//check if statuscode is range 200 -299 then success else default
            //{
            //    mcResponse.ReservationId = matchedReservation.Id;
            //}
            //else{
            //   //which id should it return
            //}
            //return mcResponse;
        }

        [HttpPost]
        [Route("AddWithCodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReservationCodeResponseDto>> AddWithCodesAsync(ReservationWithCodesDto reservationDto)
        {
            try
            {              
                ReservationCodeResponseDto responseDto = new();
                responseDto = await _reservationService.AddWithCodes(reservationDto, responseDto, false, GetLoggedInUserName);
                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in saving add with reservation code with Reservation: { JsonConvert.SerializeObject(reservationDto)}. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", null, ex.StackTrace);              
                return BadRequest(ex.Message);
            }
        }
        //[HttpPost]
        //[Route("updateLockKey")]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task UpdateLockKey()
        //{

        //    await _reservationService.UpdateLockKey();
        //}
       
        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            try
            {
                await _reservationService.DeleteAsync(id, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in deleting Reservation for id: {id}. Error: {ex.Message}.", id, ex.StackTrace);
                if (ex is System.Web.Http.HttpResponseException httpResponseException)
                {
                    if (httpResponseException.Response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        string reasonPhrase = httpResponseException.Response.ReasonPhrase;
                        _loggerService.Add(LogType.Error, $"Error occured when attempting to delete Reservation for id: {id}. User-name: {GetLoggedInUserName}. Error: {ex.Message}. Response.ReasonPhrase: {reasonPhrase}.", id, ex.StackTrace);
                        return base.Unauthorized(reasonPhrase);
                    }
                }

                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("ById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DAL.DomainEntities.BuildingUnit>> GetByIdAsync(int id)
        {
            try
            {
                DAL.DomainEntities.Reservation reservation = await _reservationService.GetByIdAsync(id);
                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in fetching Reservation for id: {id}. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", id, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("AllCurrentArches")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<CurrentReservationResponseDto>>> GetAllArchesCurrentReservationsAsync()
        {
            try
            {
                return await _reservationService.GetAllArchesCurrentReservationsAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in fetching all current Arches reservations list. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("All")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<DataSourceResultDto> GetAll(FetchRequestDto request)
        {
            try
            {
                request.SetDefaultPagination();
                DataSourceResultDto dataSourceResult = _reservationService.PagedList(request);
                return Ok(dataSourceResult);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in fetching reservations list. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("AllWithCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<DataSourceResultDto> GetAllWithCode(FetchRequestDto request)
        {
            try
            {
                request.SetDefaultPagination();
                DataSourceResultDto dataSourceResult = _reservationService.PagedList(request);
                return Ok(dataSourceResult);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in fetching reservations with codes list. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("Cancelled/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DAL.DomainEntities.BuildingUnit>> CancelledReservationAsync(int id)
        {
            try
            {
                await _reservationService.CancelledAsync(id, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in canceled Reservation for id: {id}. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", id, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("EarlyCheckIn/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DAL.DomainEntities.BuildingUnit>> EarlyCheckInAsync(int id)
        {
            try
            {
                await _reservationService.EarlyCheckInAsync(id, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in Early CheckIn Reservation for id: {id}. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", id, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("LateCheckOut/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DAL.DomainEntities.BuildingUnit>> LateCheckOutAsync(int id)
        {
            try
            {
                await _reservationService.LateCheckOutAsync(id, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in Late CheckOut Reservation for id: {id}. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", id, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }
    }
}