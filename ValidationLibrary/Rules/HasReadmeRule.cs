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
        
        public async Task<ValidationResult> IsValid(GitHubClient client, Repository gitHubRepository)
        {
            _logger.LogTrace("Rule {0} / {1}, Validating repository {2}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName);
            try
            {
                var readme = await client.Repository.Content.GetReadme("protacon", gitHubRepository.Name);
                _logger.LogDebug("Rule {0} / {1}, Validating repository {2}. Readme has content: {0}", nameof(HasReadmeRule), RuleName, gitHubRepository.FullName, readme.Content != null);
                return new ValidationResult
                {
                    RuleName = RuleName,
                    HowToFix = "Add Readme.md file to repository root with content describing this repository.",
                    IsValid = readme.Content != null
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