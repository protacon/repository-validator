using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions.GitHubDto
{
    public class Repository
    {
        public string Name { get; set; }
        public Owner Owner { get; set; }
    }
}
