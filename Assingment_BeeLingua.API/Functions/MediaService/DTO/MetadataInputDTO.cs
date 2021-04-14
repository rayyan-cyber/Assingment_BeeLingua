using Newtonsoft.Json;

namespace Assingment_BeeLingua.API.Functions.MediaService.DTO
{
    public class MetadataInputDTO
    {
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }
    }
}
