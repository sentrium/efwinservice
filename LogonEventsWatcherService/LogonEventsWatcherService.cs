using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LogonEventsWatcherService
{
    public partial class LogonEventsWatcherService : ServiceBase
    {
        private Watcher watcher = Watcher.Instance;//new Watcher();
        private Updater updater = new Updater();
        private Sender sender = new Sender();
        private ServiceHost serviceHost = null;

        public LogonEventsWatcherService()
        {
            InitializeComponent();
            Logger.Init();
        }


        protected override void OnStart(string[] args)
        {
            try
            {
                //watcher.Start();
                updater.Start();
                sender.Start();

                if (serviceHost != null)
                {
                    serviceHost.Close();
                }

                // Create a ServiceHost for the CalculatorService type and 
                // provide the base address.
                serviceHost = new ServiceHost(typeof(WindowsEventWCFService.EventWCFService));

                // Open the ServiceHostBase to create listeners and start 
                // listening for messages.
                serviceHost.Open();

            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message);
            }
        }

    
            
        protected override void OnStop()
        {
            try
            {
                watcher.Stop();
                updater.Stop();
                sender.Stop();


                if (serviceHost != null)
                {
                    serviceHost.Close();
                    serviceHost = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message);
            }
        }
    }
}
