namespace StayAccess.DTO.Doors.ArchesFrontDoor
{
    public class HomeAssistantEndPoints
    {
        public string ForUnit { get; set; }
        public string CreateFrontDoor { get; set; }
        public string UpdatePanelFrontDoor { get; set; }
        public string ExistingUserForFrontDoor { get; set; }
        public string ModifiedOrDeleteCodeForFrontDoor { get; set; }
        public string VerifyCodeForFrontDoor { get; set; }
        public string VerifyCodeForUnit { get; set; }
        public string DeleteExpiredFrontDoor { get; set; }

        public string GetExistingUserUrlForFrontDoor(string oldUnit,string oldReservation)
        {
            return string.Format(ExistingUserForFrontDoor, oldUnit, oldReservation);
        }

        public string GetModifiedOrDeleteCodeUrlForFrontDoor(int frontDoorExistingUserId)
        {
            return string.Format(ModifiedOrDeleteCodeForFrontDoor, frontDoorExistingUserId);
        }

        public string GetVerifyCodeForFrontDoor(int frontDoorExistingUserId)
        {
            return string.Format(VerifyCodeForFrontDoor, frontDoorExistingUserId);
        }
    }
}