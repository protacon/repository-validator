using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary;
using ValidationLibrary.Csv;
using ValidationLibrary.GitHub;
using ValidationLibrary.Rules;
using ValidationLibrary.Slack;
using ValidationLibrary.Utils;

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
                var githubConfig = di.GetService<GitHubConfiguration>();
                var ghClient = di.GetService<IGitHubClient>();
                var repositoryValidator = di.GetService<RepositoryValidator>();
                var client = di.GetService<ValidationClient>();
                client.Init().Wait();

                void scanner(IEnumerable<string> repositories, Options options)
                {
                    var start = DateTime.UtcNow;
                    var results = repositories.Select(repo =>
                    {
                        // This sleeps used to avoid hitting GitHub API limits
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                        return client.ValidateRepository(githubConfig.Organization, repo, options.IgnoreRepositoryRules).ConfigureAwait(false).GetAwaiter().GetResult();
                    }).ToArray();

                    ReportToConsole(logger, results);

                    if (options.AutoFix)
                    {
                        PerformAutofixes(ghClient, results);
                    }
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
                    logger.LogInformation("Duration {duration}", (DateTime.UtcNow - start).TotalSeconds);
                }

                Parser.Default.ParseArguments<ScanSelectedOptions, ScanAllOptions, DebugTestOptions>(args)
                .WithParsed<ScanSelectedOptions>(options =>
                {
                    scanner(options.Repositories, options);
                })
                .WithParsed<ScanAllOptions>(options =>
                {
                    var allNonArchivedRepositories = ghClient
                        .Repository
                        .GetAllForOrg(githubConfig.Organization)
                        .Result
                        .Where(repository => !repository.Archived);
                    scanner(allNonArchivedRepositories.Select(r => r.Name).ToArray(), options);
                })
                .WithParsed<DebugTestOptions>(options =>
                {
                    var utils = di.GetService<GitUtils>();
                    var pr = ghClient.PullRequest.Get(githubConfig.Organization, options.Repository, options.PullRequestNumber).ConfigureAwait(false).GetAwaiter().GetResult();
                    var result = utils.PullRequestHasLiveBranch(ghClient, pr).Result;
                    logger.LogInformation("PR '{title}' has live branch: {result}", pr.Title, result);
                });
            }
        }

        private static void PerformAutofixes(IGitHubClient ghClient, ValidationReport[] results)
        {
            foreach (var repositoryResult in results)
            {
                foreach (var ruleResult in repositoryResult.Results.Where(r => !r.IsValid))
                {
                    ruleResult.Fix(ghClient, repositoryResult.Repository).Wait();
                }
            }
        }

        private static void ReportToConsole(ILogger logger, params ValidationReport[] reports)
        {
            foreach (var report in reports)
            {
                logger.LogInformation($"{report.Owner}/{report.RepositoryName}");
                foreach (var error in report.Results)
                {
                    logger.LogInformation("Rule: '{ruleName}' Is valid: {isValid}", error.RuleName, error.IsValid);
                }
            }
        }

        private static async Task ReportToGitHub(IGitHubClient client, GitHubReportConfig config, ILogger<GitHubReporter> logger, params ValidationReport[] reports)
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
            using (var response = await slackClient.SendMessageAsync(reports))
            {
                var isValid = response.IsSuccessStatusCode ? "valid" : "invalid";
                logger.LogInformation("Received {isValid} response.", isValid);
            }
        }

        private static GitHubClient CreateClient(GitHubConfiguration configuration)
        {
            var client = new GitHubClient(new ProductHeaderValue("PTCS-Repository-Validator"));
            var tokenAuth = new Credentials(configuration.Token);
            client.Credentials = tokenAuth;
            return client;
        }

        private static ServiceProvider BuildDependencyInjection(IConfiguration config) => new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                })
                .AddTransient(services =>
                {
                    var githubConfig = new GitHubConfiguration();
                    config.GetSection("GitHub").Bind(githubConfig);

                    ValidateConfig(githubConfig);
                    return githubConfig;
                })
                .AddTransient<IGitHubClient, GitHubClient>(services =>
                {
                    return CreateClient(services.GetService<GitHubConfiguration>());
                })
                .AddTransient<ValidationClient>()
                .AddSingleton(provider =>
                {
                    var rules = new IValidationRule[]
                    {
                        provider.GetService<HasDescriptionRule>(),
                        provider.GetService<HasReadmeRule>(),
                        provider.GetService<HasNewestPtcsJenkinsLibRule>(),
                        provider.GetService<HasNotManyStaleBranchesRule>(),
                        provider.GetService<HasLicenseRule>()
                    };
                    return new RepositoryValidator(
                        provider.GetService<ILogger<RepositoryValidator>>(),
                        provider.GetService<IGitHubClient>(),
                        rules);
                })
                .AddTransient<GitUtils>()
                .AddTransient<HasDescriptionRule>()
                .AddTransient<HasLicenseRule>()
                .AddTransient<HasNewestPtcsJenkinsLibRule>()
                .AddTransient<HasNotManyStaleBranchesRule>()
                .AddTransient<HasReadmeRule>()
                .BuildServiceProvider();

        private static void ValidateConfig(GitHubConfiguration gitHubConfiguration)
        {
            if (gitHubConfiguration.Organization == null)
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Organization), "Organization was missing.");
            }

            if (gitHubConfiguration.Token == null)
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Token), "Token was missing.");
            }
        }
    }
}
