using Newtonsoft.Json;

namespace OpenVSSolution
{
    [JsonObject("VisualStudio")]
    public class VSSettings
    {
        [JsonProperty("Year")]
        public string Year { get; set; }
        [JsonProperty("Edition")]
        public string Edition { get; set; }
        [JsonProperty("BasePath")]
        public string BasePath { get; set; }
        [JsonProperty("IDEPath")]
        public string IDEPath { get; set; }
        [JsonProperty("Executable")]
        public string Executable { get; set; }
    }
}
