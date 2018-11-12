using System.Linq;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary
{
    public class RepositoryValidator
    {
        private IValidationRule[] _rules = new IValidationRule[]
        {
            new HasDescriptionRule()
        };

        public ValidationReport Validate(Repository repository)
        {
            var validationResults = _rules.Select(rule => rule.IsValid(repository));
            return new ValidationReport
            {
                RepositoryName = repository.FullName,
                Results = validationResults.ToArray()
            };
        }
    }
}