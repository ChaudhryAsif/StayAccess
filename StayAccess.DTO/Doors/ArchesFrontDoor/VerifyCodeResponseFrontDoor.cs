namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class VerifyCodeResponseFrontDoor
    {
        public int TotalRecords { get; set; }
        public ResultFrontDoorDto[] Results { get; set; }
        public object OrderedBy { get; set; }
        public object ResponseStatus { get; set; }
    }

    public class ResultFrontDoorDto
    {
        public object Name { get; set; }
        public string Type { get; set; }
        public int CardNumber { get; set; }
        public int PinNumber { get; set; }
        public int SiteCode { get; set; }
        public bool Disabled { get; set; }
        public int Id { get; set; }
    }
}
