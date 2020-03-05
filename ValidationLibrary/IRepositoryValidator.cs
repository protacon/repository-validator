using System.Threading.Tasks;
using Octokit;

namespace ValidationLibrary
{
    public interface IRepositoryValidator
    {
        IValidationRule[] Rules { get; }
        Task<ValidationReport> Validate(Repository gitHubRepository, bool overrideRuleIgnore);
    }
}
