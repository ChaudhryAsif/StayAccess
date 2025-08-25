using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UnitActionLogController : ControllerBase
    {
        private readonly IUnitActionLogService _unitActionLoggerService;

        private readonly ILoggerService<BuildingUnitController> _loggerService;

        public UnitActionLogController(IUnitActionLogService _unitActionLogService, ILoggerService<BuildingUnitController> loggerService)
        {
            this._unitActionLoggerService = _unitActionLogService;
            _loggerService = loggerService;
        }

        [HttpPost]
        [Route("Add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Logger>> AddAsync(UnitActionLogRequestDto unitActionLogRequest)
        {
            try
            {
                DAL.DomainEntities.UnitActionLog unitActionLog = await _unitActionLoggerService.AddAsync(unitActionLogRequest);
                _loggerService.Add(LogType.Information, $"Add Unit Action Log: sending back successful status code to API request. unitActionLogRequest: {unitActionLogRequest}.", null);
                return Ok(unitActionLog);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in Add Unit Action Log: unitActionLogRequest: {unitActionLogRequest}. Exception: {ex.ToJsonString()}.", null);
                return BadRequest("Something went wrong while saving the unit action log.");
            }
        }

        [HttpPost]
        [Route("GetLogs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<UnitActionLogRequestDto> GetLogsAsync(FetchRequestDto request)
        {
            try
            {
                request.SetDefaultPagination();
                DataSourceResultDto logs = _unitActionLoggerService.GetLogsFromRequest(request);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _ = int.TryParse(request.ReservationId, out int reservationId);
                _loggerService.Add(LogType.Error, $"Error occurred in fetching UnitActionLog records. Error: {ex.Message}.", reservationId, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

    }
}


