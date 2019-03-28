using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Octokit;
using ValidationLibrary;
using ValidationLibrary.Csv;
using ValidationLibrary.GitHub;
using ValidationLibrary.Rules;
using ValidationLibrary.Slack;

namespace Runner
{
    public class Program
    {
        private static readonly GitHubReportConfig GitHubReporterConfig = new GitHubReportConfig
        {
            Prefix = "[Automatic validation]",
            GenericNotice =
                "These issues are created, closed and reopened by [repository validator](https://github.com/protacon/repository-validator) when commits are pushed to repository. " + Environment.NewLine +
                Environment.NewLine +
                "If there are problems, please add an issue to [repository validator](https://github.com/protacon/repository-validator)" + Environment.NewLine +
                Environment.NewLine +
                "DO NOT change the name of this issue. Names are used to identify the issues created by automation." + Environment.NewLine
        };

        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            using (var di = BuildDependencyInjection(config))
            {
                var logger = di.GetService<ILogger<Program>>();
                var githubConfig = new GitHubConfiguration();
                config.GetSection("GitHub").Bind(githubConfig);

                var ghClient = CreateClient(githubConfig);
                var client = new ValidationClient(logger, ghClient);
                client.Init().Wait();

                Action<IEnumerable<string>, Options> scanner = (IEnumerable<string> repositories, Options options) => 
                {
                    var start = DateTime.UtcNow;
                    var results = repositories.Select(repo => {
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                        return client.ValidateRepository(githubConfig.Organization, repo).Result;
                    }).ToArray();

                    ReportToConsole(logger, results);

                    if (!string.IsNullOrWhiteSpace(options.CsvFile))
                    {
                        ReportToCsv(di.GetService<ILogger<CsvReporter>>(), options.CsvFile, results);
                    }
                    if (options.ReportToGithub)
                    {
                        ReportToGitHub(ghClient, GitHubReporterConfig, di.GetService<ILogger<GitHubReporter>>(), results).Wait();
                    }
                    if (options.ReportToSlack)
                    {
                        var slackSection = config.GetSection("Slack");
                        if (slackSection.Exists())
                        {
                            var slackConfig = new SlackConfiguration();
                            slackSection.Bind(slackConfig);
                            ReportToSlack(slackConfig, logger, results).Wait();
                        }
                    }
                    logger.LogInformation("Duration {0}", (DateTime.UtcNow - start).TotalSeconds);
                };

                Parser.Default.ParseArguments<ScanSelectedOptions, ScanAllOptions>(args)
                .WithParsed<ScanSelectedOptions>(options =>
                {
                    scanner(options.Repositories, options);
                })
                .WithParsed<ScanAllOptions>(options => {
                    var allRepositories = ghClient.Repository.GetAllForOrg(githubConfig.Organization).Result;
                    scanner(allRepositories.Select(r => r.Name).ToArray(), options);
                });
            }
        }

        private static void ReportToConsole(ILogger logger, params ValidationReport[] reports)
        {
            foreach (var report in reports)
            {
                logger.LogInformation($"{report.Owner}/{report.RepositoryName}");
                foreach (var error in report.Results)
                {
                    logger.LogInformation("Rule: '{0}' Is valid: {1}", error.RuleName, error.IsValid);
                }
            }
        }

        private static async Task ReportToGitHub(GitHubClient client, GitHubReportConfig config, ILogger<GitHubReporter> logger, params ValidationReport[] reports)
        {
            var reporter = new GitHubReporter(logger, client, config);
            await reporter.Report(reports);
        }

        private static void ReportToCsv(ILogger<CsvReporter> logger, string fileName, params ValidationReport[] reports)
        {
            var file = new FileInfo(fileName);
            var reporter = new CsvReporter(logger, file);
            reporter.Report(reports);
        }

        private static async Task ReportToSlack(SlackConfiguration config, ILogger logger, params ValidationReport[] reports)
        {
            var slackClient = new SlackClient(config);
            var response = await slackClient.SendMessageAsync(reports);
            var isValid = response.IsSuccessStatusCode ? "valid" : "invalid";
            logger.LogInformation($"Received {isValid} response.");
        }

        private static GitHubClient CreateClient(GitHubConfiguration configuration)
        {
            var client = new GitHubClient(new ProductHeaderValue("PTCS-Repository-Validator"));
            var tokenAuth = new Credentials(configuration.Token);
            client.Credentials = tokenAuth;
            return client;
        }

        private static ServiceProvider BuildDependencyInjection(IConfiguration config)
        {
            return new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                })
                .BuildServiceProvider();
        }
    }
}
