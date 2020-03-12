using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that a public repository has a license.
    /// 
    /// License describes how other people and organizations can use our open
    /// source projects.
    /// 
    /// See https://help.github.com/en/articles/licensing-a-repository for guidance.
    /// 
    /// License existence is only checked for public repositories.
    /// </summary>
    public class HasLicenseRule : IValidationRule
    {
        public string RuleName => "Missing License";

        private const string HowToFix = "Add a license for this repository. See [help](https://help.github.com/en/articles/licensing-a-repository) for guidance. Private repositories don't need a license.";

        private readonly ILogger<HasLicenseRule> _logger;

        public HasLicenseRule(ILogger<HasLicenseRule> logger)
        {
            _logger = logger;
        }

        public Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasLicenseRule), RuleName);
            return Task.CompletedTask;
        }

        public Task<ValidationResult> IsValid(IGitHubClient client, Repository repository)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (repository is null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasLicenseRule), RuleName, repository.FullName);
            if (repository.Private)
            {
                return Task.FromResult(new ValidationResult(RuleName, HowToFix, true, DoNothing));
            }
            if (repository.License == null)
            {
                return Task.FromResult(new ValidationResult(RuleName, HowToFix, false, DoNothing));
            }
            _logger.LogTrace("License found {key}", repository.License.Name);

            return Task.FromResult(new ValidationResult(RuleName, HowToFix, true, DoNothing));
        }

        public Dictionary<string, string> GetConfiguration()
        {
            return new Dictionary<string, string>
            {
                { "ClassName", nameof(HasLicenseRule) },
                { "RuleName", RuleName }
            };
        }

        private Task DoNothing(IGitHubClient client, Repository repository)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, No fix.", nameof(HasLicenseRule), RuleName);
            return Task.CompletedTask;
        }
    }
}
