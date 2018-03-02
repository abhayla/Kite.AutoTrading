using Kite.AutoTrading.Common.Configurations;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace Kite.AutoTrading.Common.Helper
{
    public static class ApplicationLogger
    {
        public static void LogException(string ExceptionLog)
        {
            string filepath = GetFilePath();
            //using (StreamWriter sw = File.AppendText(filepath))
            //{
            //    sw.WriteLineAsync();
            //    sw.WriteLineAsync();
            //    sw.WriteLineAsync();
            //}
            File.AppendAllText(filepath, "-----------Exception Details on " + " " + DateTime.Now.ToString() + "-----------------" + Environment.NewLine);
            File.AppendAllText(filepath, JsonConvert.SerializeObject(ExceptionLog, Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
            File.AppendAllText(filepath, "--------------------------------*End*------------------------------------------" + Environment.NewLine);
        }

        public static async Task LogJobAsync(int jobId, string message)
        {
            string filepath = GetFilePath(jobId);
            using (StreamWriter sw = File.AppendText(filepath))
                await sw.WriteLineAsync(message);
        }

        public static void LogJob(int jobId, string message)
        {
            string filepath = GetFilePath(jobId);
            //using (StreamWriter sw = File.AppendText(filepath))
            //    sw.WriteLine(message);
            File.AppendAllText(filepath, message + Environment.NewLine);
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
