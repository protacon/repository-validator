using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class PushData
    {
        [JsonProperty(PropertyName = "repository")]
        public Repository Repository { get; set; }

        [JsonProperty(PropertyName = "organization")]
        public Organization Organization { get; set; }
    }
}