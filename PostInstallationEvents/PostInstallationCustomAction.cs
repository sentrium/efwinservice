using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace PostInstallationEvents
{
    [RunInstaller(true)]
    public partial class PostInstallationCustomAction : System.Configuration.Install.Installer
    {
        string _installationDirectory = string.Empty;
        bool _createGPOs = false;
        public PostInstallationCustomAction()
        {
            InitializeComponent();
        }
        public override void Commit(IDictionary savedState)
        {
            
            base.Commit(savedState);

            _installationDirectory = Directory.GetParent(this.Context.Parameters["assemblypath"]).FullName;
            //_createGPOs = this.Context.Parameters["creategpos"].Equals("1") ? true : false;

            //
            var powerShellExecuter = new Events.PowerShellExecuter(_installationDirectory, _createGPOs);
            powerShellExecuter.ExecuteScripts();
            //
            var eventSourceLogCreator = new Events.CreateEventSourceLog();
            eventSourceLogCreator.CreatEventSource("ForwardedEvents", "ForwardedEvents");
            //
            var servicesController = new Events.WindowsServicesController();
            servicesController.RestartServices(new List<string>() { "Wecsvc" });
            //
            var fireWallRule = new Events.WindowsFirewallRule();
            fireWallRule.AddPortInboundRule(GetServicePortNumber());
        }
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            var fireWallRule = new Events.WindowsFirewallRule();
            fireWallRule.RemovePortInboundRule("Logon_Service_Port_Rule");

        }



        private string GetServicePortNumber()
        {
            var uri = string.Empty;
            var document = new XmlDocument();
            
            document.Load($@"{_installationDirectory}\LogonEventsWatcherService.exe.config");
            foreach (XmlNode element in document.GetElementsByTagName("add"))
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    if (attribute.Name == "baseAddress")
                    {
                        uri = attribute.Value;
                        break;
                    }
                }
            }

            return new Uri(uri).Port.ToString();
        }
    }
}
