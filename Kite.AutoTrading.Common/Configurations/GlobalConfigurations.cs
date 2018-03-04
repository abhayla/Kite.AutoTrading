using System;
using System.IO;

namespace Kite.AutoTrading.Common.Configurations
{
    public static class GlobalConfigurations
    {
        //Setting in global.asax
        public static string LogPath = string.Empty;

        public static TimeZoneInfo IndianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

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
