using StayAccess.DAL.DomainEntities;
using System;

namespace StayAccess.Tools.Interfaces
{
    public interface IEventLoggerService
    {
        EventLogger Add(string deviceId, int nodeId, int eventId, string eventLabel, int type, DateTime timeFired, string userId);
    }
}
