using System;
using System.Linq;

namespace StayAccess.DTO.Helpers
{
    public static class DateTimeExtension
    {
        public static DateTime ToESTDateTime(this DateTime dateTime)
        {
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTime(dateTime, easternZone);
        }

        public static DateTime FromESTToUTCDateTime(DateTime dateTime)
        {
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, easternZone);
        }

        public static bool HasTime(this DateTime dateTime)
        {
            // TimeOfDay -> 00:00:00
            return dateTime.TimeOfDay.ToString().Split(':').All(x => !x.Equals("00"));
        }

        public static DateTime GetDateTime(DateTime dateTime, string settingTime)
        {
            return !string.IsNullOrWhiteSpace(settingTime) ? Convert.ToDateTime($"{dateTime.ToShortDateString()} {settingTime}") : dateTime;
        }

        public static DateTime ConvertFromUnixTimestampToEst(int seconds)
        {
            DateTime origin = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime dateTime = origin.AddSeconds(seconds);
            return dateTime.ToESTDateTime();
        }
    }
}
