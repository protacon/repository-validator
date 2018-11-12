using System;
using Octokit;

namespace ValidationLibrary.Rules
{
    /// <summary>
    /// This rule checks that repository has a description with something in it.
    /// </summary>
    public class HasDescriptionRule : IValidationRule
    {
        public ValidationResult IsValid(Repository repository)
        {
            var isValid = !string.IsNullOrWhiteSpace(repository.Description);
            return new ValidationResult
            {
                RuleName = nameof(HasDescriptionRule),
                IsValid = isValid
            };
        }
    }
}