using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary
{
    public class RepositoryValidator
    {
        private readonly ILogger _logger;

        private readonly IValidationRule[] _rules;

        public RepositoryValidator(ILogger logger)
        {
            _rules = new IValidationRule[]
            {
                new HasDescriptionRule(logger), new HasReadmeRule(logger)
            };
            logger.LogInformation("Initializing {0} with rules: {1}", nameof(RepositoryValidator), string.Join(", ", _rules.Select(rule => rule.RuleName)));;
            _logger = logger;
        }

        public async Task<ValidationReport> Validate(GitHubClient client, Repository gitHubRepository)
        {
            _logger.LogTrace("Validatin repository {0}", gitHubRepository.FullName);
            var validationResults = await Task.WhenAll(_rules.Select(async rule => await rule.IsValid(client, gitHubRepository)));
            return new ValidationReport
            {
                Owner = gitHubRepository.Owner.Login,
                RepositoryName = gitHubRepository.Name,
                RepositoryUrl = gitHubRepository.HtmlUrl,
                Results = validationResults.ToArray()
            };
        }
    }
}