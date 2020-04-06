using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class PushData
    {
        [JsonProperty(PropertyName = "repository", Required = Required.Always)]
        public Repository Repository { get; set; }
    }
}
