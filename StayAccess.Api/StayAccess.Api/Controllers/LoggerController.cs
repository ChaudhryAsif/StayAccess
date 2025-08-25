using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StayAccess.BLL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request;
using StayAccess.DTO.Responses.Logger;
using StayAccess.Tools.Interfaces;
using System;
using StayAccess.Tools.Extensions;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoggerController : ControllerBase
    {
        private readonly ILoggerService<LoggerController> _loggerService;

        public LoggerController(ILoggerService<LoggerController> _loggerService)
        {
            this._loggerService = _loggerService;
        }

        [HttpPost]
        [Route("Add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<DAL.DomainEntities.Logger> Add(LoggerRequestDto logger)
        {
            try
            {
                DAL.DomainEntities.Logger loggerObj = _loggerService.Add(logger.LogTypeId, logger.Message, logger.ReservationId, logger.StackTrace);
                return Ok(loggerObj);
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong while saving the logger.");
            }
        }


        [HttpPost]
        [Route("GetLogs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<LoggerResponseDto> GetLogsAsync(FetchRequestDto request)
        {
            try
            {
                request.SetDefaultPagination();
                DataSourceResultDto logs = _loggerService.GetLogsFromRequest(request);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _ = int.TryParse(request.ReservationId, out int reservationId);
                _loggerService.Add(LogType.Error, $"Error occurred in fetching logger records. Error: {ex.Message}.", reservationId, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }
    }
}
