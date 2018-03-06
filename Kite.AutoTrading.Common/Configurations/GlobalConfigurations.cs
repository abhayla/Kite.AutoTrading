using System;
using System.IO;

namespace Kite.AutoTrading.Common.Configurations
{
    public static class GlobalConfigurations
    {
        //Setting in global.asax
        public static string LogPath = string.Empty;

        private static TimeZoneInfo IndianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public static DateTime IndianTime {
            get {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GlobalConfigurations.IndianTimeZone);
            }
        }

        public static string CachedDataPath
        {
            get
            {
                var filePath = LogPath + "CachedData\\";

                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);
                return filePath;
            }
        }
    }
}
