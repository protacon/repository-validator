using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class Owner
    {
        [JsonProperty(PropertyName = "login", Required = Required.Always)]
        public string Login { get; set; }
    }
}
