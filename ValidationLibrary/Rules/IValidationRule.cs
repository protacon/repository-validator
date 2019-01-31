using System;
using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// Common interface for all validation rules
    /// </summary>
    public interface IValidationRule
    {
        string RuleName { get; }

        Task<ValidationResult> IsValid(GitHubClient client, Repository repository);
    }
}
