using System;

namespace Kite.AutoTrading.Common.Configurations
{
    public static class GlobalConfigurations
    {
        //Setting in global.asax
        public static string LogPath = string.Empty;

        public static TimeZoneInfo IndianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
    }
}
