using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PostInstallationEvents.Events
{
    public class WindowsServicesController
    {
        public void RestartServices(List<string> servicesNames)
        {
            servicesNames.ForEach(serviceName =>
            {
                using (ServiceController serviceController = new ServiceController(serviceName, Environment.MachineName))
                {
                    if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        serviceController.Stop();
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                        serviceController.Start();
                    }
                }
            });
        }
    }
}
