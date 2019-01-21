using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogonEventsWatcherService.Models
{
    [JsonObject]
    class Payload
    {
        [Newtonsoft.Json.JsonProperty("mac")]
        public String Mac;

        [Newtonsoft.Json.JsonProperty("extension")]
        public String Extension;

        [Newtonsoft.Json.JsonProperty("pc")]
        public String PC;

        [Newtonsoft.Json.JsonProperty("domain")]
        public String Domain;

        [Newtonsoft.Json.JsonProperty("username")]
        public String Username;
    }
}
