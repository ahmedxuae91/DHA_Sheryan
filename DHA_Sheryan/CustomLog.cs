using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHA_Sheryan
{
    class CustomLog
    {
        private static string baseDir = ConfigurationManager.AppSettings["BaseDir"];

        public static void Info(string data)
        {
            using (StreamWriter writer = File.AppendText(baseDir + "Log\\Sheryan_Infolog.csv"))
            {
                writer.Write(System.DateTime.Now + " : " + data + "\n");
            }
        }
    }
}
