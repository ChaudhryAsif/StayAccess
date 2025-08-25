using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.HomeAssistant;
using StayAccess.Tools;
using StayAccess.Tools.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TestHomeAssistantController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHomeAssistantService _homeAssistantService;
        private readonly IReservationCodeService _reservationCodeService;
        private readonly IReservationService _reservationService;
        private readonly ILoggerService<TestHomeAssistantController> _loggerService;
        private readonly ICodeTransactionService codeTransactionService;
        private readonly IDateService _dateRepo;
        private readonly IBuildingLockSystemService _buildingLockSystemRepo;

        public TestHomeAssistantController(IConfiguration configuration, IHomeAssistantService homeAssistantService, IReservationCodeService reservationCodeService, IReservationService reservationService, ILoggerService<TestHomeAssistantController> loggerService, IDateService dateRepo, IBuildingLockSystemService buildingLockSystemRepo)
        {
            _configuration = configuration;
            _homeAssistantService = homeAssistantService;
            _reservationCodeService = reservationCodeService;
            _reservationService = reservationService;
            _loggerService = loggerService;
            _dateRepo = dateRepo;
            _buildingLockSystemRepo = buildingLockSystemRepo;
        }

        [HttpPost]
        [Route("SetDeviceCodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SetDeviceCodesAsync(CodeDto codeDto)
        {
            try
            {
                var reservation = await _reservationService.GetByIdAsync(codeDto.ReservationId);
                BuildingLockSystem buildingLockSystem = await _buildingLockSystemRepo.GetBuildingLockSystemByReservationId(codeDto.ReservationId);
                var reservationCodes = await _reservationCodeService.ListByReservationIdAsync(codeDto.ReservationId);

                string settingsStartTime = _dateRepo.GetReservationStartTimeSetting(buildingLockSystem, reservation.Id, Utilities.GetCurrentTimeInEST(), false);
                string settingsEndTime = _configuration["ReservationEndTime"];
                DateTime fromDate = !string.IsNullOrWhiteSpace(settingsStartTime) ? DateTimeExtension.GetDateTime(reservation.FromDate(), settingsStartTime) : reservation.StartDate;
                DateTime toDate = !string.IsNullOrWhiteSpace(settingsEndTime) ? DateTimeExtension.GetDateTime(reservation.ToDate(), settingsEndTime) : reservation.EndDate;

                CommandDto codes = new()
                {
                    action = "add",
                    unit = reservation?.BuildingUnit?.UnitId,
                    code = codeDto.Code,
                    from = fromDate,
                    to = toDate,
                    slot = reservationCodes.FirstOrDefault().SlotNo.ToString(),
                    reservation = reservation.Code
                };

                await _homeAssistantService.ExecuteCommandsForUnitAsync(codes, reservation.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in setting device code: {codeDto.Code}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("DeleteDeviceCodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteDeviceCodesAsync(CodeDto codeDto)
        {
            try
            {
                var reservation = await _reservationService.GetByIdAsync(codeDto.ReservationId);
                var reservationCodes = await _reservationCodeService.ListByReservationIdAsync(codeDto.ReservationId);
                CommandDto codes = new()
                {
                    action = "remove",
                    unit = reservation?.BuildingUnit?.UnitId,
                    code = codeDto.Code,
                    from = reservation.StartDate,
                    to = reservation.EndDate,
                    slot = reservationCodes.FirstOrDefault().SlotNo.ToString(),
                    reservation = reservation.Code
                };

                await _homeAssistantService.ExecuteCommandsForUnitAsync(codes, reservation.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in deleting device code: {codeDto.Code}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("SendEmailMessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SendEmailMessageAsync(string content)
        {
            try
            {
                await _homeAssistantService.SendEmailMessageAsync(content, content);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpGet]
        //[Route("ExecuteActiveCodes")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult> ExecuteActiveCodes()
        //{
        //    try
        //    {
        //        await _homeAssistantService.ExecuteActiveCodesForUnitAsync();
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpGet]
        [Route("ExecuteFailedCodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ExecuteFailedCodes()
        {
            try
            {
                await _homeAssistantService.ExecuteFailedCodesForUnitAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
