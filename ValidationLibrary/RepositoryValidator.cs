using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary
{
    public class RepositoryValidator
    {
        private IValidationRule[] _rules = new IValidationRule[]
        {
            new HasDescriptionRule(), new HasReadmeRule()
        };

        public async Task<ValidationReport> Validate(GitHubClient client, Repository gitHubRepository)
        {
            // This is simple workaround for GitHub Throttle
            Thread.Sleep(5000);
            var validationResults = await Task.WhenAll(_rules.Select(async rule => await rule.IsValid(client, gitHubRepository)));
            return new ValidationReport
            {
                RepositoryName = gitHubRepository.FullName,
                RepositoryUrl = gitHubRepository.HtmlUrl,
                Results = validationResults.ToArray()
            };
        }
    }
}