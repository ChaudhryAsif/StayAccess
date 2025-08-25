using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.Tools.Interfaces;
using System;

namespace StayAccess.Tools.Repositories
{
    public class EventLoggerService : IEventLoggerService
    {
        private readonly IGenericService<EventLogger> _eventLoggerRepo;

        public EventLoggerService(IGenericService<EventLogger> eventLoggerRepo)
        {
            _eventLoggerRepo = eventLoggerRepo;
        }

        public EventLogger Add(string deviceId, int nodeId, int eventId, string eventLabel, int type, DateTime timeFired, string userId)
        {
            EventLogger eventLogger = new() { 
                DeviceId = deviceId, 
                NodeId = nodeId, 
                EventId = eventId, 
                EventLabel = eventLabel, 
                Type = type, 
                TimeFired = timeFired,
                EventUserId = userId
            };
            _eventLoggerRepo.AddWithSave(eventLogger);

            return eventLogger;
        }

        #region public methods




        #endregion

        #region private methods

        #endregion
    }
}