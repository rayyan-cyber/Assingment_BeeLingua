using Newtonsoft.Json;
using Nexus.Base.CosmosDBRepository;
using System;
namespace Assingment_BeeLingua.DAL.Models.AMS
{
    public static class AssetAMSType
    {
        public const string Article = "Article";
        public const string Document = "Document";
        public const string Forum = "Forum";
        public const string Pdf = "Pdf";
        public const string Null = null;
        public const string Video = "Video";
    }

    public class AssetAMS : ModelBase, ICloneable
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("fileName")]
        public string Filename { get; set; }

        [JsonProperty("streamingUrl")]
        public string StreamingUrl { get; set; }

        [JsonProperty("uploadUrl")]
        public string UploadUrl { get; set; }

        [JsonProperty("contentAddress")]
        public string ContentAddress { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("createdDateUtc")]
        public DateTime? CreatedDateUtc { get; set; }


        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }
}
