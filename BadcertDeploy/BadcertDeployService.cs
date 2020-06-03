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
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading;

namespace BadcertDeploy
{
    public partial class BadcertDeployService : ServiceBase
    {
        int updateInterval = 86400;
        string dataDirectory = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert");
        string configFile = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\config.ini");
        string lastRunRecordFile = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run");

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        private System.Timers.Timer timer;
        
        public void OnDebug()
        {
            OnStart(null);
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            // If the data dir does not exist, the user must restart the service OR restore the directory.
            // If the last run record does not exist, it will be recreated.

            if (System.IO.Directory.Exists(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert")))
            {
                if (!System.IO.File.Exists(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run")))
                {
                    System.IO.File.WriteAllText(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run"), "0");
                }
                string lastRunFileContent = System.IO.File.ReadAllText(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run"));
                if (Convert.ToInt32(lastRunFileContent) + updateInterval < unixTimestamp)
                {

                    using (var client = new WebClient())
                    {
                        client.DownloadFile("https://raw.githubusercontent.com/yihanwu1024/badcert/master/badcerts.p7b",
                            Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\badcerts.p7b"));
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

                    System.IO.File.WriteAllText(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\badcert\\last-run"), Convert.ToString(unixTimestamp));
                }
            }

        }

        public BadcertDeployService()
        {
            InitializeComponent();
        }

        private void InitTimer()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStart(string[] args)
        {
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

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
            
            Thread timerThread = new Thread(new ThreadStart(this.InitTimer));
            timerThread.Start();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            timer.Close();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }
    }
}
