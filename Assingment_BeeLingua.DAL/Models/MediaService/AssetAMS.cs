using Newtonsoft.Json;
using Nexus.Base.CosmosDBRepository;
using System;
namespace Assingment_BeeLingua.DAL.Models.MediaService
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
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }


        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("fileName")]
        public string Filename { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
        // "Y" | "N"
        [JsonProperty("isAvailable")]
        public string IsAvailable { get; set; }

        [JsonProperty("contentId")]
        public string ContentId { get; set; }

        [JsonProperty("contentAddress")]
        public string ContentAddress { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusDescription")]
        public string StatusDescription { get; set; }

        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [JsonProperty("createdDateUtc")]
        public DateTime? CreatedDateUtc { get; set; }


        [JsonProperty("modifiedBy")]
        public string ModifiedBy { get; set; }

        [JsonProperty("modifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        [JsonProperty("modifiedDateUtc")]
        public DateTime? ModifiedDateUtc { get; set; }

        // "Y" | "N"
        [JsonProperty("activeFlag")]
        public string ActiveFlag { get; set; }

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }
}
