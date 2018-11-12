using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary
{
    /// <summary>
    /// Wrapper for GitHub-communication
    /// </summary>
    public class ValidationClient
    {
        private const string ProductHeader = "PTCS-Repository-Validator";
        private readonly GitHubConfiguration _configuration;

        public ValidationClient(GitHubConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<ValidationReport[]> ValidateOrganization()
        {
            var validator = new RepositoryValidator();

            var client = CreateClient();
            var allRepos = await client.Repository.GetAllForOrg(_configuration.Organization);
            return allRepos.Select(repo => validator.Validate(repo)).ToArray();
        }

        private GitHubClient CreateClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
            var tokenAuth = new Credentials(_configuration.Token);
            client.Credentials = tokenAuth;
            return client;
        }
    }
}