using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace PostInstallationEvents.Events
{
    public class PowerShellExecuter
    {
        string _installationDirectory = string.Empty;
        bool _createGPOs = false;

        public PowerShellExecuter(string installationDirectory, bool createGPOs)
        {
            this._installationDirectory = installationDirectory;
            this._createGPOs = createGPOs;
        }
        public void ExecuteScripts()
        {
            GetPSScriptsFilesPaths().ForEach(path =>
            {
                using (PowerShell powershell = PowerShell.Create())
                {
                    powershell.AddScript(@System.IO.File.ReadAllText(path).Replace("installationDirectoryPlaceHolder", _installationDirectory));
                    Collection<PSObject> results = powershell.Invoke();
                }
            });
            //if (this._createGPOs)
            //{
            //    Process process = new Process();
            //    process.StartInfo.FileName = @"gpmc.msc";
            //    process.Start();
            //}
        }

        List<string> GetPSScriptsFilesPaths()
        {
            var executingPath = $@"{ this._installationDirectory }\PS Scripts";

            var filePaths =  Directory.GetFiles(executingPath).ToList();
            filePaths.RemoveAll(path => !_createGPOs && (path.Contains("EnableWinRM") || path.Contains("Windows Events Forwarding Server")));
            filePaths.Sort();

            return filePaths;
        }

    }
}
