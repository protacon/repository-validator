using CommandLine;

namespace Runner
{
    /// <summary>
    /// Debugging help for stuff.
    /// </summary>
    [Verb("git-test", HelpText = "Debug help", Hidden = true)]
    public class GitTestOptions
    {
        [Option('r', "Repository", Required = true, HelpText = "Name of the scanned repository (without owner)")]
        public string Repository { get; set; }

        [Option("PullRequestNumber", HelpText = "Number of the Pull Request to be checked", Required = true)]
        public int PullRequestNumber { get; set; }

        public GitTestOptions()
        {
        }
    }
}