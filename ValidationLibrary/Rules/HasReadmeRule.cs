using Octokit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a proper Readme.md
    /// </summary>
    public class HasReadmeRule : GithubRuleBase<HasReadmeRule>, IValidationRule
    {
        public override string RuleName => "Missing Readme.md";

        private readonly ILogger<HasReadmeRule> _logger;

        public HasReadmeRule(ILogger<HasReadmeRule> logger, GitUtils gitUtils) : base(logger, gitUtils)
        {
            _logger = logger;
        }

        public override Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasReadmeRule), RuleName);
            return Task.CompletedTask;
        }

        protected override Task<Commit> GetCommitAsBase(IGitHubClient client, Repository repository)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            if (client is null)
            {
                throw new System.ArgumentNullException(nameof(client));
            }

            if (gitHubRepository is null)
            {
                throw new System.ArgumentNullException(nameof(gitHubRepository));
            }

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName);
            try
            {
                var readme = await client.Repository.Content.GetReadme(gitHubRepository.Owner.Login, gitHubRepository.Name).ConfigureAwait(false);
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. Readme has content: {readmeHasContent}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName, !string.IsNullOrWhiteSpace(readme.Content));
                return new ValidationResult(RuleName, "Add Readme.md file to repository root with content describing this repository.", !string.IsNullOrWhiteSpace(readme.Content), DoNothing);
            }
            catch (NotFoundException)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No Readme found, validation false.", nameof(HasReadmeRule), RuleName);
                return new ValidationResult(RuleName, "Add Readme.md file to repository root.", false, DoNothing);
            }
        }

        /// <summary>
        /// This fix creates a template request with updated Jenkinsfile
        /// </summary>
        /// <param name="client">Github client</param>
        /// <param name="repository">Repository to be fixed</param>
        private async Task Fix(IGitHubClient client, Repository repository)
        {
            throw new System.NotImplementedException();
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, No fix.", nameof(HasReadmeRule), RuleName);
            return Task.CompletedTask;
        }
    }
}