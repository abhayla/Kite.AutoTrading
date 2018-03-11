using Kite.AutoTrading.Common.Configurations;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Kite.AutoTrading.Common.Helper
{
    public static class ApplicationLogger
    {
        static ReaderWriterLock locker = new ReaderWriterLock();

        public static void LogException(string ExceptionLog)
        {
            string filepath = GetFilePath();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------Exception Details on " + " " + DateTime.Now.ToString() + "-----------------");
            sb.AppendLine(JsonConvert.SerializeObject(ExceptionLog, Newtonsoft.Json.Formatting.Indented));
            sb.AppendLine("--------------------------------*End*------------------------------------------");
            try
            {
                locker.AcquireWriterLock(int.MaxValue); 
                File.AppendAllText(filepath, sb.ToString());
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }

        //public static async Task LogJobAsync(int jobId, string message)
        //{
        //    string filepath = GetFilePath(jobId);
        //    using (StreamWriter sw = File.AppendText(filepath))
        //        await sw.WriteLineAsync(message);
        //}

        public static void LogJob(int jobId, string message)
        {
            string filepath = GetFilePath(jobId);
            try
            {
                locker.AcquireWriterLock(int.MaxValue);
                File.AppendAllText(filepath, message + Environment.NewLine);
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }
        
        private static string GetFilePath(int? jobId = null)
        {
            string filepath = string.Empty;
            if (jobId!=null && jobId.Value > 0)
                filepath = GlobalConfigurations.LogPath + "JobLog\\" + jobId.Value + "\\";
            else
                filepath = GlobalConfigurations.LogPath + "ExceptionLog\\";

            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            filepath = filepath + DateTime.Today.ToString("yyyy-MM-dd") + ".txt";   //Text File Name
            if (!File.Exists(filepath))
                File.Create(filepath).Dispose();

            return filepath;
        }
    }
}
