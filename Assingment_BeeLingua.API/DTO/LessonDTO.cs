using Newtonsoft.Json;

namespace Assingment_BeeLingua.API.DTO
{
    public class LessonDTO
    {
        [JsonProperty("lessonCode")]
        public string LessonCode { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
