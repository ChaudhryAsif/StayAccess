using System;
using System.Collections.Generic;

namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class ExistingUserResponseFrontDoorDto
    {
        public int TotalRecords { get; set; }
        public IEnumerable<Result> Results { get; set; }
        public object OrderedBy { get; set; }
        public object ResponseStatus { get; set; }
    }

    public class Result
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime ExpiresOn { get; set; }
        public bool IsMaster { get; set; }
        public bool IsSupervisor { get; set; }
        public bool IsSecurity { get; set; }
        public bool FirstCardInEnabled { get; set; }
        public bool HandicapOpener { get; set; }
        public bool CanTripleSwipe { get; set; }
        public bool CanDisengageEmergencyAlarm { get; set; }
        public int SecurityLevelId { get; set; }
        public string UserSource { get; set; }
        public Customfield[] CustomFields { get; set; }
    }

    public class Customfield
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}