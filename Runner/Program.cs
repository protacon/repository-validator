using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Octokit;
using ValidationLibrary;
using ValidationLibrary.GitHub;
using ValidationLibrary.Rules;
using ValidationLibrary.Slack;

namespace Runner
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var content = File.ReadAllText("Notice.md");
            var gitHubReporterConfig = new GitHubReportConfig 
            {
                Prefix = "[Automatic validation]",
                GenericNotice = content
            };
                
            var start = DateTime.UtcNow;
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();

            var di = BuildDependencyInjection(config);
            var logger = di.GetService<ILogger<Program>>();

            var githubConfig = new GitHubConfiguration();
            config.GetSection("GitHub").Bind(githubConfig);

            var slackConfig = new SlackConfiguration();
            config.GetSection("Slack").Bind(slackConfig);

            var ghClient = CreateClient(githubConfig);
            var client = new ValidationClient(logger, ghClient);
            var repository = client.ValidateRepository(githubConfig.Organization, "validation-test-repository").Result;
            ReportToGitHub(ghClient, gitHubReporterConfig, logger, repository).Wait();
            ReportToConsole(logger, repository);
            ReportToSlack(slackConfig, logger, repository).Wait();
            logger.LogInformation("Duration {0}", (DateTime.UtcNow - start).TotalSeconds);
            di.Dispose();
        }

        private static void ReportToConsole(ILogger logger, params ValidationReport[] reports)
        {
            foreach (var report in reports)
            {
                logger.LogInformation($"{report.Owner}/{report.RepositoryName}");
                foreach (var error in report.Results)
                {
                    logger.LogInformation("{0} {1}", error.RuleName, error.IsValid);
                }
            }
        }

        private static async Task ReportToGitHub(GitHubClient client, GitHubReportConfig config, ILogger logger, params ValidationReport[] reports)
        {
            var reporter = new GitHubReporter(logger, client, config);
            await reporter.Report(reports);
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
                .AddLogging(loggingBuilder => {
                    loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                })
                .BuildServiceProvider();
        }
    }
}
