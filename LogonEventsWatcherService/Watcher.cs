using LogonEventsWatcherService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;

using System.Threading.Tasks;
using System.Timers;

namespace LogonEventsWatcherService
{
    class Watcher
    {
        private static Watcher _watcher = null;

        public static Watcher Instance {
            get {
                if (_watcher == null)
                    _watcher = new Watcher();

                return _watcher;
            }
        }

        private Watcher()
        {


        }


       // private ManagementEventWatcher managementEventWatcher;
        private Dictionary<String, int> previousEvents = new Dictionary<String, int>();
        private Dictionary<String, String> userComputers = new Dictionary<String, String>();
        private Dictionary<String, Dictionary<String, String>> userLogonIDs = new Dictionary<String, Dictionary<String,String>>(StringComparer.InvariantCultureIgnoreCase);

        private Timer timer;
        private int LastIndex = -100;

        public void Start()
        {
            //ManagementScope managementScope = new ManagementScope(Constants.ManagementConnectionString);
            //managementScope.Connect();

            //managementEventWatcher = new ManagementEventWatcher(managementScope, new EventQuery(Constants.ManagementQuery));
            //managementEventWatcher.EventArrived += new EventArrivedEventHandler(eventArrived);
            //managementEventWatcher.Start();


            //Logger.Log.Info("Watcher. Event logs");
            //foreach (var eventLog in EventLog.GetEventLogs())
            //{
            //    Logger.Log.Info("Display name:" + eventLog.LogDisplayName + ", mashine name:" + eventLog.MachineName);
            //}

            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = int.Parse(ConfigurationManager.AppSettings["WatcherTimerInterval"]) * 1000;
            //timer.Start();

            Logger.Log.Info("Watcher started");
        }
        public void AddEventInQueue(Models.PSEventLogEntry entry)
        {
            ParseEventEntry(entry);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            EventLog log = new EventLog("ForwardedEvents");
            // Console.WriteLine("Time: " + DateTime.Now.ToString());
            //var cnt = 0;
            foreach (var entry in log.Entries.Cast<EventLogEntry>()
                                     .Where(x => x.TimeGenerated > DateTime.Now.AddMinutes(-1))
                                     .Select(x => new
                                     {
                                         x.Index,
                                         x.InstanceId,
                                         x.MachineName,
                                         x.UserName,
                                         x.Message,
                                         x.TimeGenerated,
                                         x.ReplacementStrings
                                     }))
            {
                if (entry.Index > LastIndex)
                {
                    //Console.WriteLine("Index: " + entry.Index.ToString() + ", event ID: " + entry.InstanceId + ", time: " + entry.TimeGenerated.ToString() + ", computer: " + entry.MachineName);
                    //cnt++;
                    ParseEventEntry(entry);
                    LastIndex = entry.Index;
                }
            }
            //Console.WriteLine("Total: " + cnt.ToString());
        }

        public void Stop()
        {
            //if (managementEventWatcher != null)
            //    managementEventWatcher.Stop();

            timer.Stop();
            Logger.Log.Info("Watcher stopped");
            GC.Collect();
        }

        private void eventArrived(object sender, EventArrivedEventArgs e)
        {
            PropertyData pd;
            if ((pd = e.NewEvent.Properties["TargetInstance"]) != null)
            {
                ManagementBaseObject mbo = pd.Value as ManagementBaseObject;
                ProcessManagementObject(mbo);
            }
        }
        private void ParseEventEntry(PSEventLogEntry entry)
        {
            try
            {
                String accoutName = "";
                String logonID = "";
                String domainName = "";

                if (entry.Action.Equals("logon",StringComparison.InvariantCultureIgnoreCase))
                {
                    accoutName = entry.Username;
                    domainName = entry.UserDomain;
                    logonID = string.Empty;//entry.ReplacementStrings[7];

                    if (userLogonIDs.ContainsKey(accoutName) && userLogonIDs[accoutName].ContainsKey(entry.ComputerName))
                    {
                        return;
                    }
                    else if (!userLogonIDs.ContainsKey(accoutName))
                    {
                        userLogonIDs[accoutName] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    }
                   
                    userLogonIDs[accoutName].Add(entry.ComputerName, logonID);

                }
                else if (entry.Action.Equals("logoff",StringComparison.InvariantCultureIgnoreCase))
                {
                    accoutName = entry.Username;
                    domainName = entry.UserDomain;
                    logonID = string.Empty;//entry.ReplacementStrings[3];


                    if (!userLogonIDs.ContainsKey(accoutName) || (userLogonIDs.ContainsKey(accoutName) && !userLogonIDs[accoutName].ContainsKey(entry.ComputerName)))
                        return;

                    if (userLogonIDs.ContainsKey(accoutName) && userLogonIDs[accoutName].ContainsKey(entry.ComputerName))
                        userLogonIDs[accoutName].Remove(entry.ComputerName);


                    var zombieUserNames = userLogonIDs.ToList().Where(x => x.Value.Count == 0).Select(x => x.Key).ToList();
                    zombieUserNames.ForEach(name => userLogonIDs.Remove(name));

                }

                if (domainName == Constants.WindowManager)
                    return;

                EventData eventData = new EventData();
                //eventData.EventCode = (int)entry.InstanceId;
                eventData.ActionName = entry.Action;
                eventData.TimeGenerated = DateTime.UtcNow;
                eventData.AccountName = accoutName;
                eventData.DomainName = domainName;
                eventData.LogonID = logonID;
                eventData.ComputerName = entry.ComputerName.Split('.')[0].ToUpper();

                if (!String.IsNullOrEmpty(eventData.AccountName) && eventData.AccountName != Constants.System)
                {
                    if (String.IsNullOrEmpty(eventData.ComputerName) && userComputers.ContainsKey(eventData.AccountName))
                        eventData.ComputerName = userComputers[eventData.AccountName];

                    if (!String.IsNullOrEmpty(eventData.ComputerName))
                    {
                        String logString = String.Format("Watcher. Enqueue action: {0}, username: {1}, computer: {2}, domain: {3}, time: {4}",
                             eventData.ActionName, eventData.AccountName, eventData.ComputerName, eventData.DomainName, eventData.TimeGenerated.ToString());
                        Logger.Log.Info(logString);

                        Queue.Enqueue(eventData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(Utils.FormatStackTrace(new StackTrace()) + ": " + ex.Message);
            }
        }

        private void ParseEventEntry(dynamic entry)
        {
            try
            {
                String accoutName = "";
                String logonID = "";
                String domainName = "";

                if (entry.InstanceId == Constants.LogonEventCode)
                {
                    accoutName = entry.ReplacementStrings[5];
                    domainName = entry.ReplacementStrings[6];
                    logonID = entry.ReplacementStrings[7];

                    if (userLogonIDs.ContainsKey(accoutName))
                        return;

                    //userLogonIDs.Add(accoutName, logonID);
      
                }
                else if (entry.InstanceId == Constants.LogoffEventCode)
                {
                    accoutName = entry.ReplacementStrings[1];
                    domainName = entry.ReplacementStrings[2];
                    logonID = entry.ReplacementStrings[3];

                    if (!userLogonIDs.ContainsKey(accoutName))
                        return;

                    userLogonIDs.Remove(accoutName);
                }

                if (domainName == Constants.WindowManager)
                    return;

                EventData eventData = new EventData();
                eventData.EventCode = (int)entry.InstanceId;
                eventData.TimeGenerated = entry.TimeGenerated;
                eventData.AccountName = accoutName;
                eventData.DomainName = domainName;
                eventData.LogonID = logonID;
                eventData.ComputerName = entry.MachineName.Split('.')[0].ToUpper();

                if (!String.IsNullOrEmpty(eventData.AccountName) && eventData.AccountName != Constants.System)
                {
                    if (String.IsNullOrEmpty(eventData.ComputerName) && userComputers.ContainsKey(eventData.AccountName))
                        eventData.ComputerName = userComputers[eventData.AccountName];

                    if (!String.IsNullOrEmpty(eventData.ComputerName))
                    {
                        String logString = String.Format("Watcher. Enqueue action: {0}, username: {1}, computer: {2}, domain: {3}, time: {4}",
                             eventData.ActionName, eventData.AccountName, eventData.ComputerName, eventData.DomainName, eventData.TimeGenerated.ToString());
                        Logger.Log.Info(logString);

                        Queue.Enqueue(eventData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(Utils.FormatStackTrace(new StackTrace()) + ": " + ex.Message);
            } 
        }

        private void ProcessManagementObject(ManagementBaseObject mbo)
        {
            try
            {
                EventData eventData = new EventData();
                var eventCodeProp = mbo.Properties["EventCode"];
                if (eventCodeProp != null)
                    eventData.EventCode =  int.Parse(eventCodeProp.Value.ToString());
             
                var timeGeneratedProp = mbo.Properties["TimeGenerated"];
                if (timeGeneratedProp.Value != null)
                    eventData.TimeGenerated = ManagementDateTimeConverter.ToDateTime(timeGeneratedProp.Value.ToString());

                var messageProp = mbo.Properties["Message"];
                if (messageProp != null)
                    parseMessage(messageProp.Value.ToString(), eventData);


                if (!String.IsNullOrEmpty(eventData.AccountName) && eventData.AccountName != Constants.System)
                {
                    if (String.IsNullOrEmpty(eventData.ComputerName) && userComputers.ContainsKey(eventData.AccountName))
                        eventData.ComputerName = userComputers[eventData.AccountName];

                    //String id = eventData.AccountName + "|" + eventData.ComputerName;

                    //// Check if we sending even with same code twice
                    //bool b = false;
                    //if (!previousEvents.ContainsKey(id))
                    //{
                    //    previousEvents.Add(id, eventData.EventCode);
                    //    b = true;
                    //}
                    //else
                    //{
                    //    if (previousEvents[id] != eventData.EventCode)
                    //    {
                    //        previousEvents[id] = eventData.EventCode;
                    //        b = true;
                    //    }
                    //}

                    //if (b)
                    //{
                        if (!String.IsNullOrEmpty(eventData.ComputerName))
                        {
                            String logString = String.Format("Watcher. Enqueue action: {0}, username: {1}, computer: {2}, domain: {3}, time: {4}",
                                 eventData.ActionName, eventData.AccountName, eventData.ComputerName, eventData.DomainName, eventData.TimeGenerated.ToString());
                            Logger.Log.Info(logString);

                            Queue.Enqueue(eventData);
                        }
  //                  }                   
                }
            }
            catch(Exception ex)
            {
                Logger.Log.Error(Utils.FormatStackTrace(new StackTrace()) + ": " + ex.Message); 
            }           
       }

        private void parseMessage(String message, EventData eventData)
        {
            String[] parts = message.Split("\r\t\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (eventData.EventCode == Constants.LogonEventCode)
            {
                Logger.Log.Info("Watcher. Analyze event " + Constants.LogonEventCode);
                Logger.Log.Info(message);
                String sourceNetworkAddress = "";
                String workstationName = "";
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == Constants.NewLogon)
                    {
                        eventData.LogonID = parts[i + 8];
                        Logger.Log.Info("Watcher. LogonID: " + eventData.LogonID);

                        if (parts[i + 2] == Constants.System)
                            return;

                        String accountName = parts[i + 4];
                        if (userLogonIDs.ContainsKey(accountName))
                            return;

                        if (accountName[accountName.Length - 1] == '$') // this is computer account
                            return;

                        eventData.AccountName = accountName;
                        eventData.DomainName = parts[i + 6];
                    

                        Logger.Log.Info("Watcher. Parsed logon: account:  " + eventData.AccountName 
                            + ", domain: " + eventData.DomainName +", logon ID: " + eventData.LogonID);
                       
                       // userLogonIDs.Add(eventData.AccountName, eventData.LogonID);
                  }
                    else if (parts[i] == Constants.WorkstationName)
                    {
                        Logger.Log.Info("Watcher. Parsed Workstation Name: " + parts[i + 1]);
                        workstationName = parts[i + 1];
                    }
                    else if (parts[i] == Constants.SourceNetworkAddress)
                    {
                        Logger.Log.Info("Watcher. Parsed Source Network Address: " + parts[i + 1]);
                        sourceNetworkAddress = parts[i + 1];
                    }
                }

                if (!String.IsNullOrEmpty(workstationName) && workstationName != "-")
                {
                    eventData.ComputerName = workstationName.ToUpper();
                }
                else if (!String.IsNullOrEmpty(sourceNetworkAddress) && sourceNetworkAddress != "-")
                {
                    String hostname = Dns.GetHostEntry(sourceNetworkAddress).HostName;
                    if (!String.IsNullOrEmpty(hostname))
                    {
                        eventData.ComputerName = hostname.Split('.')[0].ToUpper();
                        String logString = String.Format("Watcher. Name from source network address: {0}, hostname: {1} ", sourceNetworkAddress, eventData.ComputerName);
                        Logger.Log.Info(logString);
                    }
                }

                if (!String.IsNullOrEmpty(eventData.AccountName) && !String.IsNullOrEmpty(eventData.ComputerName))
                {
                     if (!userComputers.ContainsKey(eventData.AccountName))
                        userComputers.Add(eventData.AccountName, eventData.ComputerName);

                    userComputers[eventData.AccountName] = eventData.ComputerName;
                    Logger.Log.Info("Watcher. Add computer " + eventData.ComputerName + " for user + " + eventData.AccountName);
                }
            }
            else if (eventData.EventCode == Constants.LogoffEventCode)
            {
                Logger.Log.Info("Watcher. Analyze event " + Constants.LogoffEventCode);
                Logger.Log.Info(message);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == Constants.Subject)
                    {
                        String logonID = parts[i + 8];
                        Logger.Log.Info("Watcher. LogonID: " + logonID);

                        String accountName = parts[i + 4];
                        if (!userLogonIDs.ContainsKey(accountName))
                            return;
                                              
                        //if (logonID != userLogonIDs[accountName])
                        //    return;

                        eventData.AccountName = accountName;
                        eventData.DomainName = parts[i + 6];
                        eventData.LogonID = logonID;

                        Logger.Log.Info("Watcher. Parsed logoff: account:  " + eventData.AccountName
                            + ", domain: " + eventData.DomainName + ", logon ID: " + eventData.LogonID);

                        userLogonIDs.Remove(eventData.AccountName);
                        break;
                    }
                }
            }
        }
        

    }
}
