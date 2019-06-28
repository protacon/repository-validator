using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Runner
{
    [Verb("scan-all", HelpText = "Scans all repositories for owner")]
    public class ScanAllOptions : Options
    {
        public ScanAllOptions(bool reportToSlack, bool reportToGithub, bool autoFix, string csvFile) : base(reportToSlack, reportToGithub, autoFix, csvFile)
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
                    new Example("Scan all repositories and only report to console", ExampleSettings, new ScanAllOptions(false, false, false, null)),
                    new Example("Scan all repositories and report to Slack", ExampleSettings, new ScanAllOptions(true, false, false, null)),
                    new Example("Scan all repositories and report to GitHub", ExampleSettings, new ScanAllOptions(false, true, false, null)),
                    new Example("Scan all repositories and report to GitHub and Slack", ExampleSettings, new ScanAllOptions(true, true, false, null)),
                    new Example("Scan all repositories and create pull requests", ExampleSettings, new ScanAllOptions(false, false, true, null))
                };
            }
        }
    }
}