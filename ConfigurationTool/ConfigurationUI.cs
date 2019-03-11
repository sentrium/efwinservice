using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ConfigurationTool
{
    public partial class ConfigurationUI : Form
    {
        string _serviceName = string.Empty;
        public ConfigurationUI(string serviceName)
        {
            this._serviceName = serviceName;

            InitializeComponent();

            //SetInfoIfServiceNameNotGiven();
            SetJoinedDomainPath();
        }

        private void SetInfoIfServiceNameNotGiven()
        {
            //SetJoinedDomainPath();
            //if (string.IsNullOrEmpty(this._serviceName))
            //{
            //    var executingDirectoryPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath);

            //    var configFilePath = $@"{executingDirectoryPath}\LogonEventsWatcherService.exe.config";
            //}
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtLdapPath.Text))
            {
                errorProvider.SetError(txtLdapPath, "Please give Ldap Path.");
                return;
            }
            if (string.IsNullOrEmpty(txtWebURL.Text) || !IsValidWEBURL(txtWebURL.Text))
            {
                errorProvider.SetError(txtWebURL, "Please give valid Web URL.");
                return;
            }
            if (string.IsNullOrEmpty(txtToken.Text))
            {
                errorProvider.SetError(txtToken, "Please give Ldap Path.");
                return;
            }
            if (!IsValidPortNumber(txtServicePort.Text))
            {
                errorProvider.SetError(txtServicePort, "Please give valid port number.");
                return;
            }
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(GetServiceConfigFilePath());

            foreach (XmlNode element in xmlDocument.GetElementsByTagName("add"))
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    if (attribute.Name == "key" && attribute.Value == "LdapPath")
                    {
                        foreach (XmlAttribute attributeToUpdate in element.Attributes)
                        {
                            if (attributeToUpdate.Name == "value")
                            {
                                attributeToUpdate.Value = txtLdapPath.Text;
                            }
                        }
                    }
                    if (attribute.Name == "key" && attribute.Value == "TargetWebServiceUrl")
                    {
                        foreach (XmlAttribute attributeToUpdate in element.Attributes)
                        {
                            if (attributeToUpdate.Name == "value")
                            {
                                attributeToUpdate.Value = txtWebURL.Text;
                            }
                        }
                    }
                    if (attribute.Name == "key" && attribute.Value == "AuthToken")
                    {
                        foreach (XmlAttribute attributeToUpdate in element.Attributes)
                        {
                            if (attributeToUpdate.Name == "value")
                            {
                                attributeToUpdate.Value = txtToken.Text;
                            }
                        }
                    }
                    if (attribute.Name == "baseAddress")
                    {
                        attribute.Value = $"http://localhost:{txtServicePort.Text}/Design_Time_Addresses/LogonEventsWatcherService.WindowsEventWCFService/EventWCFService/";
                    }
                }

            }

            xmlDocument.Save(GetServiceConfigFilePath());

            ServiceController sc = new ServiceController(this._serviceName);
            sc.WaitForStatus(ServiceControllerStatus.Running);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                sc.Start();
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool IsValidPortNumber(string portNumber)
        {
            var result = Regex.Match(portNumber, "^([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$", RegexOptions.IgnoreCase);

            return result.Success;
        }

        string GetServiceConfigFilePath()
        {
            return $@"{System.IO.Path.GetDirectoryName(Application.ExecutablePath)}\LogonEventsWatcherService.exe.config";
            //using (ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + _serviceName + "'"))
            //{
            //    wmiService.Get();
            //    string currentserviceExePath = wmiService["PathName"].ToString();
            //    return $"{wmiService["PathName"].ToString().Replace("\"", "")}.config";
            //}
        }
        void SetJoinedDomainPath()
        {
            var domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            domainName = string.Join(",", domainName.Split(".".ToArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => $"dc={x}"));

            txtLdapPath.Text = $"LDAP://{domainName}";
        }

        bool IsValidWEBURL(string URLString)
        {
            Uri uriResult;
            return Uri.TryCreate(URLString, UriKind.Absolute, out uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp
                   || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        
    }
}
