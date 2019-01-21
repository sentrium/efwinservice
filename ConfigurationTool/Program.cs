using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConfigurationTool
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string serviceName = string.Empty;
            if (args.Count() > 0)
                serviceName = args[0];

            Application.Run(new ConfigurationUI(serviceName));
        }
    }
}
