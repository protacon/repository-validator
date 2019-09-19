using System.Threading.Tasks;

namespace ValidationLibrary
{
    public interface IValidationClient
    {
        Task Init();
        Task<ValidationReport> ValidateRepository(string organization, string repositoryName, bool overrideRuleIgnore);
    }
}