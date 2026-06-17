using System;
using System.IO;

namespace KPKflowConsole.Logs
{
    public class Log
    {
        public static void WriteLog(string sEvent)
        {
            StreamWriter log;
            if (!File.Exists("logfile.txt"))
            {
                log = new StreamWriter("logfile.txt");
            }
            else
            {
                log = File.AppendText("logfile.txt");
            }

            log.WriteLine(DateTime.Now + ":  --" + sEvent);
            log.Close();
        }

    }
}
