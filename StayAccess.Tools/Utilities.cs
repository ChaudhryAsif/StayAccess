using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools
{
    public static class Utilities
    {
        public static DateTime GetCurrentTimeInEST()
        {
            try
            {
                TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTime currentEstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
                return currentEstTime;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
