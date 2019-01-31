using System;
using Octokit;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a proper Readme.md
    /// </summary>
    public class HasReadmeRule : IValidationRule
    {
        private const string RuleName = "Missing Readme.md";

        public async Task<ValidationResult> IsValid(GitHubClient client, Repository gitHubRepository)
        {
            try
            {
                var readme = await client.Repository.Content.GetReadme("protacon", gitHubRepository.Name);
                return new ValidationResult
                {
                    RuleName = RuleName,
                    HowToFix = "Add Readme.md file to repository root with content describing this repository.",
                    IsValid = readme.Content != null
                };
            } 
            catch (Octokit.NotFoundException)
            {
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