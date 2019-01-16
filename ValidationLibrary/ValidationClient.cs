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
            
            //var r = await client.Repository.Get("protacon", "repository-validator");
            //var z = await client.Repository.Get("protacon", "barfoo-testicles");
            //var allRepos = new []{r, z};
            var allRepos = await client.Repository.GetAllForOrg(_configuration.Organization);
            var results = await Task.WhenAll(allRepos.Select(repo => validator.Validate(client, repo)));
            return results.ToArray();
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