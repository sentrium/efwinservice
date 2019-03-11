using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LogonEventsWatcherService.Models
{
    [DataContract]
    public class PSEventLogEntry
    {
        [DataMember(Name = "action")]
        public string Action = string.Empty;
        [DataMember(Name = "username")]
        public string Username = string.Empty;
        [DataMember(Name = "userdomain")]
        public string UserDomain = string.Empty;
        [DataMember(Name = "computername")]
        public string ComputerName = string.Empty;
        [DataMember(Name = "computerdomain")]
        public string ComputerDomain = string.Empty;
        
    }
}
