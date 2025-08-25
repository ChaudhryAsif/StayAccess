using System;
using System.Collections.Generic;

namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class CreateRequestFrontDoorDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime ExpiresOn { get; set; }
        public int SecurityLevelId { get; set; }
        public bool IsMaster { get; set; }
        public bool IsSupervisor { get; set; }
        public bool CanTripleSwipe { get; set; }
        public bool CanDisengageEmergencyAlarm { get; set; }
        public bool HandicapOpener { get; set; }
        public string CardholderImage { get; set; }
        public string AccessoryImage1 { get; set; }
        public string AccessoryImage2 { get; set; }
        public int UserSource { get; set; }
        public string AlternateId1 { get; set; }
        public string AlternateId2 { get; set; }
        public IEnumerable<CustomFields> CustomFields { get; set; }
        public IEnumerable<Cards> Cards { get; set; }
        public IEnumerable<int> AccessGroups { get; set; }
        public IEnumerable<int> Partitions { get; set; }
        public int CodeTransactionId { get; set; }
    }

    public class CustomFields
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Cards
    {
        public int SiteCode { get; set; }
        public int CardNumber { get; set; }
        public string PinNumber { get; set; }
    }
}
