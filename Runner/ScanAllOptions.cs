using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Runner
{
    [Verb("scan-all", HelpText = "Scans all repositories for owner")]
    public class ScanAllOptions : Options
    {
        public ScanAllOptions(bool reportToSlack, bool reportToGithub, bool autoFix, string csvFile, bool ignoreRepositoryRules) : base(reportToSlack, reportToGithub, autoFix, csvFile, ignoreRepositoryRules)
        {
        }

        private static readonly IEnumerable<UnParserSettings> ExampleSettings = new[]
        {
            new UnParserSettings { PreferShortName = false }
        };

        [Usage(ApplicationAlias = "dotnet run --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>
                {
                    new Example("Scan all repositories and only report to console", ExampleSettings, new ScanAllOptions(false, false, false, null, false)),
                    new Example("Scan all repositories and report to Slack", ExampleSettings, new ScanAllOptions(true, false, false, null, false)),
                    new Example("Scan all repositories and report to GitHub", ExampleSettings, new ScanAllOptions(false, true, false, null, false)),
                    new Example("Scan all repositories and report to GitHub and Slack", ExampleSettings, new ScanAllOptions(true, true, false, null, false)),
                    new Example("Scan all repositories and create pull requests", ExampleSettings, new ScanAllOptions(false, false, true, null, false)),
                    new Example("Scan all repositories, create pull requests while ignoring repository specific configurations. This is not recommended!", ExampleSettings, new ScanAllOptions(false, false, true, null, false))
                };
            }
        }
    }
}