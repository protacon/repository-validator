using System;
using System.Linq;
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
        private readonly GitHubClient _client;
        private readonly RepositoryValidator _validator;

        public ValidationClient(ILogger logger, GitHubClient client)
        {
            _client = client;
            _validator = new RepositoryValidator(logger);
        }

        public async Task Init()
        {
            await _validator.Init(_client);
        }

        public async Task<ValidationReport> ValidateRepository(string organization, string repositoryName)
        {
            var repository = await _client.Repository.Get(organization, repositoryName);
            var result = await _validator.Validate(_client, repository);
            return result;
        }
    }
}