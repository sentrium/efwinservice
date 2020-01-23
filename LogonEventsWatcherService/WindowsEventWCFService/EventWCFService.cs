using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace LogonEventsWatcherService.WindowsEventWCFService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "EventWCFService" in both code and config file together.
    public class EventWCFService : IEventWCFService
    {
        public void RegisterEvent(Models.PSEventLogEntry eventLogEntry)
        {
            Watcher.Instance.AddEventInQueue(eventLogEntry);
        }
    }
}
