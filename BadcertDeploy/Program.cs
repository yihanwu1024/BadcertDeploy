using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BadcertDeploy
{
    static class Program
    {
        static void Main()
        {
#if DEBUG
            BadcertDeployService debugService = new BadcertDeployService();
            debugService.OnDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new BadcertDeployService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
