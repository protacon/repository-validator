using System;
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
        private readonly GitHubClient _client;
        public ValidationClient(GitHubClient client)
        {
            _client = client;
        }

        public async Task<ValidationReport[]> ValidateOrganization(string organization)
        {
            var validator = new RepositoryValidator();

            var allRepos = await _client.Repository.GetAllForOrg(organization);
            var results = await Task.WhenAll(allRepos.Select(repo => validator.Validate(_client, repo)));
            return results.ToArray();
        }

        public async Task<ValidationReport> ValidateRepository(string organization, string repositoryName)
        {
            var validator = new RepositoryValidator();

            var repository = await _client.Repository.Get(organization, repositoryName);
            var result = await validator.Validate(_client, repository);
            return result;
        }
    }
}