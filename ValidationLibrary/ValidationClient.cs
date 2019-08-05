using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ValidationLibrary
{
    /// <summary>
    /// Wrapper for GitHub-communication
    /// </summary>
    public class ValidationClient
    {
        private readonly IGitHubClient _client;
        private readonly RepositoryValidator _validator;

        public ValidationClient(ILogger logger, IGitHubClient client, RepositoryValidator validator)
        {
            _client = client;
            _validator = validator;
        }

        public async Task Init()
        {
            await _validator.Init();
        }

        public async Task<ValidationReport> ValidateRepository(string organization, string repositoryName)
        {
            var repository = await _client.Repository.Get(organization, repositoryName);
            var result = await _validator.Validate(repository);
            return result;
        }
    }
}