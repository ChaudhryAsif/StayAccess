using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StayAccess.BLL.Interfaces;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request.Latch;
using StayAccess.DTO.Responses;
using StayAccess.Tools.Interfaces;
using System;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LockKeyController : BaseController
    {
        private readonly ILockKeyService _lockKeyRepo;
        private readonly ILoggerService<LockKeyController> _loggerRepo;

        public LockKeyController(ILockKeyService lockKeyRepo, ILoggerService<LockKeyController> loggerRepo)
        {
            _lockKeyRepo = lockKeyRepo;
            _loggerRepo = loggerRepo;
        }

        [HttpPost]
        [Route("Save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaveResponseDto>> SaveAsync(LockKeyRequestDto lockKeyRequestDto)
        {
            try
            {

                SaveResponseDto response = new();

                var matchedReservation = await _lockKeyRepo.GetMatchedLockKeyAsync(lockKeyRequestDto);
                if (matchedReservation != null)
                {
                    response.Id = matchedReservation.Id;
                }
                else
                {
                    if (lockKeyRequestDto.Id > 0)
                    {
                        await _lockKeyRepo.UpdateAsync(lockKeyRequestDto, GetLoggedInUserName);
                        response.Id = lockKeyRequestDto.Id;
                    }
                    else
                    {
                        DAL.DomainEntities.LockKey lockKey = _lockKeyRepo.Add(lockKeyRequestDto, GetLoggedInUserName);
                        if (lockKey != null)
                            response.Id = lockKey.Id;
                    }
                }
               
                return Ok(response);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in saving Lock Key: {JsonConvert.SerializeObject(lockKeyRequestDto)}. Error: {ex.Message}.", null, ex.StackTrace);
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
                await _lockKeyRepo.DeleteAsync(id, GetLoggedInUserName);
                return Ok();
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in deleting Lock key for id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("ById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DAL.DomainEntities.LockKey>> GetByIdAsync(int id)
        {
            try
            {
                DAL.DomainEntities.LockKey lockKey = await _lockKeyRepo.GetByIdAsync(id);
                return Ok(lockKey);
            }
            catch (Exception ex)
            {
                _loggerRepo.Add(LogType.Error, $"Error occurred in fetching Lock key for id: {id}. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

    }
}
