using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class Owner
    {
        [JsonProperty(PropertyName = "login")]
        public string Login { get; set; }
    }
}