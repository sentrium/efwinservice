using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostInstallationEvents.Events
{
    public class CreateEventSourceLog
    {
        public void CreatEventSource(string eventSource, string eventLog)
        {
            if (!System.Diagnostics.EventLog.SourceExists(eventSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(eventSource, eventLog);
            }
        }
    }
}
