using System;
using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary
{
    /// <summary>
    /// Validation result for single validation rule
    /// </summary>
    public class ValidationResult
    {
        public string RuleName { get; }
        public string HowToFix { get; }
        public bool IsValid { get; }

        public Func<GitHubClient, Repository, Task> Fix { get; }

        public ValidationResult(string ruleName, string howToFix, bool isValid, Func<GitHubClient, Repository, Task> fix)
        {
            RuleName = ruleName;
            HowToFix = howToFix;
            IsValid = isValid;
            Fix = fix;
        }
    }
}
