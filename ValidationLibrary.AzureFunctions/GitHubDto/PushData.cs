using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class PushData
    {
        [JsonProperty(PropertyName = "repository")]
        public Repository Repository { get; set; }
    }
}