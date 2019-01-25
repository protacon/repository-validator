using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class Organization
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}