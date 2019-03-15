using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Runner
{
    [Verb("scan-all", HelpText = "Scans all repositories for owner")]
    public class ScanAllOptions : Options
    {
        public ScanAllOptions(bool reportToSlack, bool reportToGithub, string csvFile) : base(reportToSlack, reportToGithub, csvFile)
        {
        }

        private static IEnumerable<UnParserSettings> ExampleSettings = new[]
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
                    new Example("Scan all repositories and only report to console", ExampleSettings, new ScanAllOptions(false, false, null)),
                    new Example("Scan all repositories and report to Slack", ExampleSettings, new ScanAllOptions(true, false, null)),
                    new Example("Scan all repositories and report to GitHub", ExampleSettings, new ScanAllOptions(false, true, null)),
                    new Example("Scan all repositories and report to GitHub and Slack", ExampleSettings, new ScanAllOptions(true, true, null)),
                };
            }
        }
    }
}