using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Request;
using StayAccess.Tools.Interfaces;
using System;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventLoggerController : ControllerBase
    {
        private readonly IEventLoggerService _eventLoggerService;

        public EventLoggerController(IEventLoggerService _eventLoggerService)
        {
            this._eventLoggerService = _eventLoggerService;
        }

        [HttpPost]
        [Route("Add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<EventLogger> Add(EventLoggerRequestDto eventLogger)
        {
            try
            {
                EventLogger loggerObj = _eventLoggerService.Add( 
                    eventLogger.DeviceId, 
                    eventLogger.NodeId, 
                    eventLogger.EventId, 
                    eventLogger.EventLabel, 
                    eventLogger.Type, 
                    eventLogger.TimeFired,
                    eventLogger.UserId);
                return Ok(loggerObj);
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong while saving the event logger.");
            }
        }
    }
}
