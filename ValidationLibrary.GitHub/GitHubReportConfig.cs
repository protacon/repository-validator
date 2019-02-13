namespace ValidationLibrary.GitHub
{
    public class GitHubReportConfig
    {
        /// <summary>
        /// This notice is added to every issue created by GitHub reporter.
        /// 
        /// 1. This should be markdown
        /// 2. This should contain general info about how validation is done
        /// </summary>
        public string GenericNotice { get; set; }

        /// <summary>
        /// This prefix is used to indicate that issues and comment created
        /// by reporter are automated
        /// </summary>
        public string Prefix { get; set; }
    }
}