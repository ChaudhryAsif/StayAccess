using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StayAccess.Arches.Interfaces;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request;
using StayAccess.DTO.Responses;
using StayAccess.DTO.Responses.Arches;
using StayAccess.Latch.Interfaces;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BuildingUnitController : BaseController
    {
        private readonly IBuildingUnitService _buildingUnitService;
        private readonly IHomeAssistantService _homeAssistantService;
        private readonly ILatchService _latchService;
        private readonly ILoggerService<BuildingUnitController> _loggerService;

        public BuildingUnitController(IBuildingUnitService buildingUnitService, IHomeAssistantService homeAssistantService, ILoggerService<BuildingUnitController> loggerService, ILatchService latchService)
        {
            _buildingUnitService = buildingUnitService;
            _homeAssistantService = homeAssistantService;
            _latchService = latchService;
            _loggerService = loggerService;
        }

        [HttpPost]
        [Route("Save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaveResponseDto>> SaveAsync(BuildingUnitRequestDto buildingUnitdto)
        {
            try
            {
                SaveResponseDto response = new SaveResponseDto();

                if (buildingUnitdto.Id > 0)
                {
                    await _buildingUnitService.UpdateAsync(buildingUnitdto, GetLoggedInUserName);
                    response.Id = buildingUnitdto.Id;
                }
                else
                {
                    DAL.DomainEntities.BuildingUnit buildingUnitd = await _buildingUnitService.AddAsync(buildingUnitdto, GetLoggedInUserName);
                    if (buildingUnitd != null)
                        response.Id = buildingUnitd.Id;
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in saving Building Unit: {JsonConvert.SerializeObject(buildingUnitdto)}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("CaptureCodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CaptureCodesAsync(List<CaptureCodesRequestDto> requestDtos)
        {
            try
            {
                _loggerService.Add(LogType.Information, $"Capturing building unit codes: {JsonConvert.SerializeObject(requestDtos)}.", null);
                await _buildingUnitService.CaptureCodesAsync(requestDtos);
                _loggerService.Add(LogType.Information, $"Building unit codes captured successfully -> {JsonConvert.SerializeObject(requestDtos)}.", null);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in capturing codes: {JsonConvert.SerializeObject(requestDtos)}. Error: {ex.Message}.", null, ex.StackTrace);
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
                await _buildingUnitService.DeleteAsync(id, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in deleting Building Unit for id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
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
                DAL.DomainEntities.BuildingUnit buildingUnit = await _buildingUnitService.GetByIdAsync(id);
                return Ok(buildingUnit);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in fetching Building Unit for id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
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
                DataSourceResultDto dataSourceResult = _buildingUnitService.PagedList(request);
                return Ok(dataSourceResult);
            }
            catch (Exception ex)
            {

                _loggerService.Add(LogType.Error, $"Error occured in fetching Building Units list. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get dropdown
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Dropdown")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<DropdownDto>>> GetDropdownAsync()
        {
            try
            {
                List<DropdownDto> buildingUnits = await _buildingUnitService.GetDropdownAsync();
                return Ok(buildingUnits);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occurred in fetching Building Unit dropdown. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all unit log and unit slot log records
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("UnitLogs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DataSourceResultDto>> GetUnitLogsAsync(FetchRequestDto request)
        {
            try
            {
                request.SetDefaultPagination();
                DataSourceResultDto result = await _buildingUnitService.GetPageListUnitLogs(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in fetching Unit Logs. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteCode/{unitSlotLogId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteCodeAsync(int unitSlotLogId)
        {
            try
            {
                bool isDeleted = false;
                        isDeleted = await _homeAssistantService.DeleteArchesBuildingUnitCodeAsync(unitSlotLogId);
                if (isDeleted)
                    return Ok();
                else
                    return BadRequest("Building Unit Code Deletion Has Failed.");
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in deleting Building Unit Code for unitSlotLogId: {unitSlotLogId}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }
    }
}