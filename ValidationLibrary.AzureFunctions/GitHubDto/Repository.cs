using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class Repository
    {
        [JsonProperty(PropertyName = "name")]
        public string Name {get;set;}
    }
}