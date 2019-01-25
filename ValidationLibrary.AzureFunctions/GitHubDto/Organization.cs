using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class Organization
    {
        [JsonProperty(PropertyName = "login")]
        public string Login { get; set; }
    }
}