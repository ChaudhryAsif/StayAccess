using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Reservations;
using StayAccess.DTO.Responses;
using StayAccess.DTO.Responses.Arches;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReservationCodeController : BaseController
    {
        private readonly IReservationCodeService _reservationCodeService;
        private readonly IReservationService _reservationService;
        private readonly IHomeAssistantService _homeAssistantService;
        private readonly ILoggerService<ReservationCodeController> _loggerService;
        private readonly ICodeTransactionService _codeTransactionService;
        private readonly IBuildingLockSystemService _buildingLockSystemRepo;

        public ReservationCodeController(IReservationCodeService reservationCodeService, IReservationService reservationService, IHomeAssistantService homeAssistantService, ILoggerService<ReservationCodeController> loggerService, ICodeTransactionService codeTransactionService, IBuildingLockSystemService buildingLockSystemRepo)
        {
            _reservationCodeService = reservationCodeService;
            _reservationService = reservationService;
            _homeAssistantService = homeAssistantService;
            _loggerService = loggerService;
            _codeTransactionService = codeTransactionService;
            _buildingLockSystemRepo = buildingLockSystemRepo;
        }

        [HttpPost]
        [Route("Save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaveResponseDto>> SaveAsync(ReservationCodeDto reservationCodeDto)
        {
            try
            {
                SaveResponseDto response = new();
                List<ReservationCode> reservationCodes = null;

                var reservation = await _reservationService.GetByIdAsync(reservationCodeDto.ReservationId);
                if (reservation is null) throw new Exception("Reservation not found.");

                BuildingLockSystem buildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystem(reservation);

                if (buildingLockSystem == BuildingLockSystem.Latch) throw new Exception("Cannot edit lockcode for this building's reservations.");

                if (reservationCodeDto.Id > 0)
                {
                    // update reservation code in database
                    await _reservationCodeService.UpdateAsync(reservationCodeDto, GetLoggedInUserName);

                    response.Id = reservationCodeDto.Id;

                    // get latest reservation codes
                    reservationCodes = await _reservationCodeService.ListByReservationIdAsync(reservation.Id);

                    // Delete device lock code on HomeAssistant api
                    //_homeAssistantService.DeleteCodesForUnit(reservation, reservationCodes, true);
                }
                else
                {
                    reservationCodes = await _reservationCodeService.ListByReservationIdAsync(reservation.Id);

                    //if (buildingLockSystem != BuildingLockSystem.Latch || (buildingLockSystem == BuildingLockSystem.Latch && reservationCodes.Count == 0))
                    //{
                    _reservationService.AdjustReservationCodeDto(reservationCodeDto, buildingLockSystem);
                    var reservationCode = await _reservationCodeService.AddAsync(reservationCodeDto, buildingLockSystem, reservation, GetLoggedInUserName);
                    if (reservationCode != null)
                    {
                        response.Id = reservationCode.Id;
                        reservationCodes = new List<ReservationCode> { reservationCode };
                    }
                    //}
                    //else if (buildingLockSystem == BuildingLockSystem.Latch && reservationCodes.Count > 0)
                    //{
                    //    response.Id = reservationCodes.Select(x => x.Id).Single();
                    //}
                }
                //if (reservation.HasValidDates())
                //{
                //    // Set device lock codes on HomeAssistant api, if reservation is active
                //    _homeAssistantService.SetCodesForUnit(reservation, reservationCodes);
                //}

                return Ok(response);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in saving Reservation Code: {JsonConvert.SerializeObject(reservationCodeDto)}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            try
            {
                ReservationCode reservationCode = await _reservationCodeService.GetByIdAsync(id);
                if (reservationCode != null)
                {
                    if (reservationCode.Status == CodeStatus.Pending || reservationCode.Status == CodeStatus.Active)
                    {
                        // Delete device lock code on HomeAssistant api
                        //_homeAssistantService.DeleteCodesForUnit(reservationCode.Reservation, new List<ReservationCode> { reservationCode });

                        var reservation = await _reservationService.GetByIdAsync(reservationCode.ReservationId);
                        BuildingLockSystem reservationBuildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystem(reservation);

                        if (reservationCode.Status == CodeStatus.Pending)
                        {
                            // create transactions for pending codes, in case of reservation code is deleted
                            await _codeTransactionService.CreatePendingCodeTransactionsForInActiveAsync(reservation, GetLoggedInUserName, reservationBuildingLockSystem);
                        }
                        else if (reservationCode.Status == CodeStatus.Active)
                        {
                            // create transactions for active codes, in case of reservation code is deleted
                            _codeTransactionService.CreateDeleteCodeTransactionsForInActive(reservation, GetLoggedInUserName, reservationBuildingLockSystem);
                        }

                        // start executing pending transactions
                        _codeTransactionService.ExecuteTransactions(true);
                    }

                    // delete reservation code from database
                    _reservationCodeService.Delete(reservationCode);
                }
                else
                {
                    throw new Exception("Reservation code not found");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in deleting Reservation Code for Id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("ById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReservationCode>> GetByIdAsync(int id)
        {
            try
            {
                ReservationCode reservationCode = await _reservationCodeService.GetByIdAsync(id);
                return Ok(reservationCode);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in fetching Reservation Code for id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
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
                DataSourceResultDto dataSourceResult = _reservationCodeService.PagedList(request);
                return Ok(dataSourceResult);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in fetching Reservation Codes list. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }
    }
}