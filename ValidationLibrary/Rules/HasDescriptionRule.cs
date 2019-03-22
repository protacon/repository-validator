using System;
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

        private readonly ILogger _logger;

        public HasDescriptionRule(ILogger logger)
        {
            _logger = logger;
        }

        public Task Init(IGitHubClient ghClient)
        {
            return Task.FromResult(0);
        }

        public Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            _logger.LogTrace("Rule {0} / {1}, Validating repository {2}", nameof(HasDescriptionRule), RuleName, gitHubRepository.FullName);
            var isValid = !string.IsNullOrWhiteSpace(gitHubRepository.Description);
            _logger.LogDebug("Rule {0} / {1}, Validating repository {2}. Has description: {3}", nameof(HasDescriptionRule), RuleName, gitHubRepository.FullName, isValid);
            return Task.FromResult(new ValidationResult
            {
                RuleName = RuleName,
                HowToFix = "Add description for this repository.",
                IsValid = isValid
            });
        }
    }
}