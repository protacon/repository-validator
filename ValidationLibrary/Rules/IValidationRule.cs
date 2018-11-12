using System;
using Octokit;

namespace ValidationLibrary.Rules
{
    public interface IValidationRule
    {
        ValidationResult IsValid(Repository repository);
    }
}
