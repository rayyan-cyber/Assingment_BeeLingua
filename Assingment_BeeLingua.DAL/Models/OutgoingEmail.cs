using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assingment_BeeLingua.DAL.Models
{
    public class OutgoingEmail
    {
        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("attachment")]
        public byte[] Attachment { get; set; }

        [JsonProperty("attachmentName")]
        public string AttachmentName { get; set; }
    }
}
