using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ErrorLoggingService
    {
        public void Log(string context, Exception ex)
        {
            try
            {
                string logsDir = Path.Combine(Application.StartupPath, "logs");
                Directory.CreateDirectory(logsDir);
                string logFile = Path.Combine(logsDir, "fweledit-errors.log");
                using (StreamWriter sw = new StreamWriter(logFile, true, Encoding.UTF8))
                {
                    sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + context);
                    if (ex != null)
                    {
                        sw.WriteLine(ex.GetType().FullName + ": " + ex.Message);
                        sw.WriteLine(ex.StackTrace ?? string.Empty);
                    }
                    sw.WriteLine();
                }
            }
            catch
            {
            }
        }
    }
}
