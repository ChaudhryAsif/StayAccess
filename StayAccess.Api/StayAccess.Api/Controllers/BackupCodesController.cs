using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StayAccess.BLL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.ReservationCode;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using StayAccess.DTO.Request.CodeBackupDto.cs;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BackupCodesController : BaseController
    {
        private readonly ILoggerService<ReservationController> _loggerService;
        private readonly IBackupCodesService _backupCodesService;

        public BackupCodesController(ILoggerService<ReservationController> loggerService, IBackupCodesService backupCodesService)
        {
            _loggerService = loggerService;
            _backupCodesService = backupCodesService;
        }

        [HttpGet]
        [Route("Set")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReservationCodeResponseDto>> SetBackupsAsync()
        {
            try
            {
                await _backupCodesService.HandleArchesBackups(GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in HandleArchesBackups. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", null, ex.StackTrace);
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
                DataSourceResultDto dataSourceResult = _backupCodesService.PagedList(request);
                return Ok(dataSourceResult);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in fetching backup reservations list. Request: {request.ToJsonString()}. User-name: {GetLoggedInUserName}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("MarkUsed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DataSourceResultDto>> MarkUsedAsync(MarkUsedRequestDto markUsedRequestDto)
        {
            try
            {
                if (markUsedRequestDto.BackupReservationId == default)
                    throw new Exception("Invalid backupReservationId for mark used request.");

                await _backupCodesService.MarkAsUsedAsync(markUsedRequestDto.BackupReservationId, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in marking backup reservation as used. Id passed in: {markUsedRequestDto.BackupReservationId} User-name: {GetLoggedInUserName}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

    }
}
