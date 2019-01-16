using System;
using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a description with something in it.
    /// </summary>
    public class HasDescriptionRule : IValidationRule
    {
        public Task<ValidationResult> IsValid(GitHubClient client, Repository gitHubRepository)
        {
            var isValid = !string.IsNullOrWhiteSpace(gitHubRepository.Description);
            return Task.FromResult(new ValidationResult
            {
                RuleName = nameof(HasDescriptionRule),
                IsValid = isValid
            });
        }
    }
}