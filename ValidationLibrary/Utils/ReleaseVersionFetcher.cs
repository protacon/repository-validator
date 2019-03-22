using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary.Utils
{
    public class ReleaseVersionFetcher
    {
        private IGitHubClient _client;
        private string _owner;
        private string _name;

        public ReleaseVersionFetcher(IGitHubClient client, string owner, string name)
        {
            _client = client;
            _owner = owner;
            _name = name;
        }

        public async Task<string> GetLatest()
        {
            var result = await _client.Repository.Release.GetLatest(_owner, _name);
            return result.TagName;
        }
    }
}