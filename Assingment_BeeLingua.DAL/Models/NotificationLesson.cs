using Newtonsoft.Json;
using Nexus.Base.CosmosDBRepository;
using System;

namespace Assingment_BeeLingua.DAL.Models
{
    public class NotificationLesson : ModelBase
    {
        // TODO : bersihkan comment yang tdk terpakai : DONE

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("eventTime")]
        public DateTime EventTime { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("messageBodyEvent")]
        public string MessageBodyEvent { get; set; }
    }
}
