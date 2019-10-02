using CommandLine;

namespace Runner
{
    /// <summary>
    /// Debugging help for tool development. Append when needed
    /// </summary>
    [Verb("git-test", HelpText = "Checks if PR has live branch.", Hidden = true)]
    public class DebugTestOptions
    {
        [Option('r', "Repository", Required = true, HelpText = "Name of the scanned repository (without owner)")]
        public string Repository { get; set; }

        [Option("PullRequestNumber", HelpText = "Number of the Pull Request to be checked", Required = true)]
        public int PullRequestNumber { get; set; }
    }
}