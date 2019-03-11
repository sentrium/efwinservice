using LogonEventsWatcherService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogonEventsWatcherService
{
    class Sender
    {
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        private AutoResetEvent resetEvent = new AutoResetEvent(false);

        public Sender()
        {
            backgroundWorker.DoWork += DoWork;
            backgroundWorker.WorkerSupportsCancellation = true;
        }

        public void Start()
        {
           backgroundWorker.RunWorkerAsync();
           Logger.Log.Info("Sender started");
        }

        public void Stop()
        {
            Queue.Cancel();

            backgroundWorker.CancelAsync();
            resetEvent.WaitOne();
            Logger.Log.Info("Sender stopped");
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            Logger.Log.Info("Sender do work");
            while (!e.Cancel)
            {
                EventData eventData = Queue.Dequeue();
                if (eventData != null)
                {
                    String logString = String.Format("Sender. Dequeue action: {0}, username: {1}, computer: {2}, domain: {3}, time: {4}",
                        eventData.ActionName, eventData.AccountName,eventData.ComputerName, eventData.DomainName, eventData.TimeGenerated.ToString());
                    Logger.Log.Info(logString);
                    Logger.Log.Info("Sender. Queue count: " + Queue.Count.ToString());

                    Send(eventData);
                }

                if (backgroundWorker.CancellationPending)
                    break;

            }
            resetEvent.Set();
        }

        private void Send(EventData eventData)
        {
            try
            {
                Logger.Log.Info("Sender. Try to find user in cache: " + eventData.AccountName);
                UserData userData = Cache.UserData[eventData.AccountName];
                Logger.Log.Info("Sender. User found");

                String computerName = eventData.ComputerName.Split('.')[0];
                Logger.Log.Info("Sender. Try to find computer in cache: " + computerName);
                ComputerData computerData = Cache.ComputerData[computerName];
                Logger.Log.Info("Sender. Computer found");

                var requestData = new RequestData()
                {
                    ID = Guid.NewGuid().ToString(),
                    Type = eventData.ActionName.Equals("logon",StringComparison.InvariantCultureIgnoreCase) ?
                        Constants.AdUserLogin : Constants.AdUserLogout,
                    TimeStamp = (eventData.TimeGenerated.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
                    
                    Publisher = Constants.Publisher,
                    Payload = new Payload()
                    {
                        Mac = computerData.Mac,
                        
                        Extension = userData.Extension,
                        PC = eventData.ComputerName,
                        Domain = eventData.DomainName,
                        Username = eventData.AccountName
                    }
                };

                string json = JsonConvert.SerializeObject(requestData);

                var request = WebRequest.Create(ConfigurationManager.AppSettings["TargetWebServiceUrl"]);
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Headers.Add("Authorization", "Bearer " + ConfigurationManager.AppSettings["AuthToken"]);
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                Logger.Log.Info("Sender. Perform http request with payload: " + json);

                var response = request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    Logger.Log.Info("Sender. Respose: " + result);
                }
            }
            catch(Exception ex)
            {
                Logger.Log.Error(Utils.FormatStackTrace(new StackTrace()) + ": " + ex.Message);
            }
        }
    }
}
