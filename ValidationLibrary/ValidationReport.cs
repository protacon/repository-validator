using Octokit;

namespace ValidationLibrary
{
    /// <summary>
    /// Summary containing all validation results for repository
    /// </summary>
    public class ValidationReport
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string RepositoryUrl { get; set; }
        public Repository Repository { get; set; }
        public ValidationResult[] Results { get; set; }
    }
}