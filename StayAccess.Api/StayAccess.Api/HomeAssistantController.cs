using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.DTO.Enums;
using StayAccess.DTO.HomeAssistant;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StayAccess.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HomeAssistantController : Controller
    {
        private readonly IHomeAssistantService _homeAssistantService;
        private readonly ILoggerService<HomeAssistantController> _loggerService;
        private readonly ICodeTransactionService _codeTransactionService;

        public HomeAssistantController(IHomeAssistantService homeAssistantService, ILoggerService<HomeAssistantController> loggerService, ICodeTransactionService codeTransactionServicee)
        {
            _homeAssistantService = homeAssistantService;
            _loggerService = loggerService;
            _codeTransactionService = codeTransactionServicee;
        }

        [HttpPost]
        [Route("SetDeviceCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SetDeviceCodeAsync(CommandDto commandDto)
        {
            try
            {
                commandDto.action = "add";

                _homeAssistantService.AdjustCommmandDtoDatesTmeOfDay(commandDto);

                bool isSuccess = await _homeAssistantService.ExecuteCommandsForUnitAsync(commandDto, null);
                if (isSuccess)
                    return Ok();
                else
                    throw new Exception("API response status code was unsuccessful");
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in setting device code: {commandDto.code} . CommandDto: {commandDto.ToJsonString()}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("SaveCodeTransactionError")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult SaveCodeTransactionError(ErrorDto error)
        {
            try
            {
                _codeTransactionService.SaveCodeTransactionError(error);
                return Ok("Error saved successfully.");
            }
            catch (Exception e)
            {
                _loggerService.Add(LogType.Error, $"Error occurred while saving node red errors: {JsonConvert.SerializeObject(error)}. Error: {e.Message}.", null, e.StackTrace);
                return BadRequest(e.Message);
            }
        }
    }
}
