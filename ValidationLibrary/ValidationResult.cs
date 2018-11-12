namespace ValidationLibrary
{
    /// <summary>
    /// Validation result for single validation rule
    /// </summary>
    public class ValidationResult
    {
        public string RuleName { get; set; }
        public bool IsValid { get; set; }
    }
}