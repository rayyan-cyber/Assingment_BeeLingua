using Newtonsoft.Json;

namespace Assingment_BeeLingua.API.Functions.MediaService.DTO
{
    public class MetadataOutputDTO
    {
        [JsonProperty("resourceId")]
        public string ResourceId { get; set; }

        [JsonProperty("contentAddress")]
        public string ContentAddress { get; set; }

        [JsonProperty("uploadUrl")]
        public string UploadUrl { get; set; }
    }
}
