using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a description with something in it.
    /// </summary>
    public class HasDescriptionRule : IValidationRule
    {
        public string RuleName => "Missing description";

        private readonly ILogger<HasDescriptionRule> _logger;

        public HasDescriptionRule(ILogger<HasDescriptionRule> logger)
        {
            _logger = logger;
        }

        public Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasDescriptionRule), RuleName);
            return Task.CompletedTask;
        }

        public Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            if (gitHubRepository is null)
            {
                throw new System.ArgumentNullException(nameof(gitHubRepository));
            }

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasDescriptionRule), RuleName, gitHubRepository.FullName);
            var isValid = !string.IsNullOrWhiteSpace(gitHubRepository.Description);
            _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. Has description: {hasDescription}", nameof(HasDescriptionRule), RuleName, gitHubRepository.FullName, isValid);
            return Task.FromResult(new ValidationResult(RuleName, "Add description for this repository.", isValid, DoNothing));
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, No fix.", nameof(HasDescriptionRule), RuleName);
            return Task.CompletedTask;
        }
    }
}