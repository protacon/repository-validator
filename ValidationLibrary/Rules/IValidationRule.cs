using System;
using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary.Rules
{
    public interface IValidationRule
    {
        Task<ValidationResult> IsValid(GitHubClient client, Repository repository);
    }
}
