using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StayAccess.BLL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Helpers;
using StayAccess.DTO.Request;
using StayAccess.DTO.Responses;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BuildingController : BaseController
    {

        private readonly IBuildingService _buildingRepo;
        private readonly ILoggerService<LockKeyController> _loggerRepo;

        public BuildingController(IBuildingService buildingRepo, ILoggerService<LockKeyController> loggerRepo)
        {
            _buildingRepo = buildingRepo;
            _loggerRepo = loggerRepo;
        }


        [HttpPost]
        [Route("Save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaveResponseDto>> SaveAsync(BuildingRequestDto buildingRequestDto)
        {
            try
            {

                if (!Enum.IsDefined(typeof(BuildingLockSystem), buildingRequestDto.BuildingLockSystem))
                    throw new Exception("Invalid BuildingLockSystem");

                SaveResponseDto response = new();

                var matchedReservation = await _buildingRepo.GetMatchedBuildingAsync(buildingRequestDto);
                if (matchedReservation != null)
                {
                    response.Id = matchedReservation.Id;
                }
                else
                {
                    if (buildingRequestDto.Id > 0)
                    {
                        await _buildingRepo.UpdateAsync(buildingRequestDto, GetLoggedInUserName);
                        response.Id = buildingRequestDto.Id;
                    }
                    else
                    {
                        DAL.DomainEntities.Building building = _buildingRepo.Add(buildingRequestDto, GetLoggedInUserName);
                        if (building != null)
                            response.Id = building.Id;
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in saving Building: {JsonConvert.SerializeObject(buildingRequestDto)}. Error: {ex.Message}.", null, ex.StackTrace);
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
                await _buildingRepo.DeleteAsync(id, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in deleting Building for id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("ById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DAL.DomainEntities.Building>> GetByIdAsync(int id)
        {
            try
            {
                DAL.DomainEntities.Building building = await _buildingRepo.GetByIdAsync(id);
                return Ok(building);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in fetching Building for id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
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
                List<DropdownDto> buildings = await _buildingRepo.GetDropdownAsync();
                return Ok(buildings);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in fetching Building drop-down. Error: {ex.Message}.", null, ex.StackTrace);
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
                DataSourceResultDto dataSourceResult = _buildingRepo.PagedList(request);
                return Ok(dataSourceResult);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in fetching reservations list. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

    }
}
