using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Runner
{
    [Verb("scan-selected", HelpText = "Scans selected repositories")]
    public class ScanSelectedOptions : Options
    {
        [Option('r', "Repository", Required = true, HelpText = "Name of the scanned repositories (without owner(s))")]
        public IEnumerable<string> Repositories { get; }

        public ScanSelectedOptions(IEnumerable<string> repositories, bool reportToSlack, bool reportToGithub, bool autofix, string csvFile) : base(reportToSlack, reportToGithub, autofix, csvFile)
        {
            Repositories = repositories;
        }

        private static readonly IEnumerable<UnParserSettings> ExampleSettings = new[]
        {
            new UnParserSettings { PreferShortName = true },
            new UnParserSettings { PreferShortName = false }
        };

        [Usage(ApplicationAlias = "dotnet run --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>
                {
                    new Example("Scan repository called 'repository-validator' and only report to console",
                        ExampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, false, false, false, null)),
                    new Example("Scan repository called 'repository-validator' and report to Slack",
                        ExampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, true, false, false, null)),
                    new Example("Scan repository called 'repository-validator' and report to GitHub",
                        ExampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, false, true, false, null)),
                    new Example("Scan repository called 'repository-validator' and report to GitHub and Slack",
                        ExampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, true, true, false, null)),
                    new Example("Scan repositories called 'repository-validator', 'repository-2' and 'repository-3' then report to CSV file",
                        ExampleSettings, new ScanSelectedOptions(new []{"repository-validator", "repository-2", "repository-3"}, false, false, false, "results.csv")),
                    new Example("Scan repository called 'repository-validator' and create pull request if needed.",
                        ExampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, false, false, true, null)),

                };
            }
        }
    }
}