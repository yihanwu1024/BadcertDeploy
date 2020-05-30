using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BadcertDeploy
{
    public partial class BadcertDeployService : ServiceBase
    {
        public BadcertDeployService()
        {
            InitializeComponent();

            int updateInterval = 86400;
            string dataDirectory = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert");
            string configFile = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\config.ini");
            string lastRunRecordFile = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run");

            #region Create the data directory, config file and last run record if they do not exist

            if (System.IO.Directory.Exists(dataDirectory) == false)
            {
                System.IO.Directory.CreateDirectory(dataDirectory);
            }

            if (System.IO.File.Exists(configFile) == false)
            {
                System.IO.File.Create(configFile);
            }

            if (System.IO.File.Exists(lastRunRecordFile) == false)
            {
                System.IO.File.WriteAllText(lastRunRecordFile, "0");
            }

            #endregion


            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            string lastRunFileContent = System.IO.File.ReadAllText(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run"));

            if (Convert.ToInt32(lastRunFileContent) < unixTimestamp + updateInterval) { 

                using (var client = new WebClient())
                {
                    client.DownloadFile("https://raw.githubusercontent.com/yihanwu1024/badcert/master/badcerts.p7b", Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\badcerts.p7b"));
                }

                #region Run certutil command to install certificates

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C certutil -addstore -ent Disallowed " + Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\badcerts.p7b");
                process.StartInfo = startInfo;
                process.Start();

                #endregion

                lastRunFileContent = Convert.ToString(unixTimestamp);
                System.IO.File.WriteAllText(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run"), lastRunFileContent);

            }
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }
    }
}
