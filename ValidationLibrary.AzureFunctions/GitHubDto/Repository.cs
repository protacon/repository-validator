using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class Repository
    {
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "owner", Required = Required.Always)]
        public Owner Owner { get; set; }
    }
}
