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
            return Task.FromResult(0);
        }

        public async Task<ValidationResult> IsValid(IGitHubClient client, Repository gitHubRepository)
        {
            _logger.LogTrace("Rule {0} / {1}, Validating repository {2}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName);
            try
            {
                var readme = await client.Repository.Content.GetReadme("protacon", gitHubRepository.Name);
                _logger.LogDebug("Rule {0} / {1}, Validating repository {2}. Readme has content: {3}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName, !string.IsNullOrWhiteSpace(readme.Content));
                return new ValidationResult
                {
                    RuleName = RuleName,
                    HowToFix = "Add Readme.md file to repository root with content describing this repository.",
                    IsValid = !string.IsNullOrWhiteSpace(readme.Content)
                };
            } 
            catch (Octokit.NotFoundException)
            {
                _logger.LogDebug("No Readme found, validation false.");
                return new ValidationResult
                {
                    RuleName = RuleName,
                    HowToFix = "Add Readme.md file to repository root.",
                    IsValid = false
                };
            }
        }
    }
}