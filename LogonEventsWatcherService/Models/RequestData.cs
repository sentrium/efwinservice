using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogonEventsWatcherService.Models
{
    [JsonObject]
    class RequestData
    {
        [Newtonsoft.Json.JsonProperty("id")]
        public String ID;

        [Newtonsoft.Json.JsonProperty("type")]
        public String Type;

        [Newtonsoft.Json.JsonProperty("timestamp")]
        public Double TimeStamp;

        [Newtonsoft.Json.JsonProperty("publisher")]
        public String Publisher;

        [Newtonsoft.Json.JsonProperty("payload")]
        public Payload Payload;

    }
}
