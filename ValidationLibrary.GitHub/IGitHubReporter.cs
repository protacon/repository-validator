using System.Collections.Generic;
using System.Threading.Tasks;

namespace ValidationLibrary.GitHub
{
    public interface IGitHubReporter
    {
        Task Report(IEnumerable<ValidationReport> reports);
    }
}
