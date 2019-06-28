using System;
using Octokit;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a proper Readme.md
    /// </summary>
    public class HasReadmeRule : IValidationRule
    {
        public string RuleName =>  "Missing Readme.md";

        private readonly ILogger _logger;

        public HasReadmeRule(ILogger logger)
        {
            _logger = logger;
        }
        
        public Task Init(IGitHubClient ghClient)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, Initialized", nameof(HasReadmeRule), RuleName);
            return Task.FromResult(0);
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            _logger.LogTrace("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName);
            try
            {
                var readme = await client.Repository.Content.GetReadme(gitHubRepository.Owner.Login, gitHubRepository.Name);
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, Validating repository {repositoryName}. Readme has content: {readmeHasContent}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName, !string.IsNullOrWhiteSpace(readme.Content));
                return new ValidationResult
                {
                    RuleName = RuleName,
                    HowToFix = "Add Readme.md file to repository root with content describing this repository.",
                    IsValid = !string.IsNullOrWhiteSpace(readme.Content),
                    Fix = Fix
                };
            } 
            catch (Octokit.NotFoundException)
            {
                _logger.LogDebug("Rule {ruleClass} / {ruleName}, No Readme found, validation false.", nameof(HasReadmeRule), RuleName);
                return new ValidationResult
                {
                    RuleName = RuleName,
                    HowToFix = "Add Readme.md file to repository root.",
                    IsValid = false,
                    Fix = Fix
                };
            }
        }

        public Task Fix(IGitHubClient client, Repository repository)
        {
            _logger.LogInformation("Rule {ruleClass} / {ruleName}, No fix.", nameof(HasReadmeRule), RuleName);
            return Task.FromResult(0);
        }
    }
}