using System.Collections.Generic;

namespace ValidationLibrary
{
    /// <summary>
    /// Summary containing all validation results for repository
    /// </summary>
    public class ValidationReport
    {
        public string RepositoryName { get; set; }
        public string RepositoryUrl { get; set; }
        public ValidationResult[] Results { get; set; }
    }
}