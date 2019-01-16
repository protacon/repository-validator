using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Octokit;
using ValidationLibrary;
using ValidationLibrary.Rules;
using ValidationLibrary.Slack;

namespace Runner
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var start = DateTime.UtcNow;
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            IConfiguration  config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();
            
            var githubConfig = new GitHubConfiguration();
            config.GetSection("GitHub").Bind(githubConfig);

            var slackConfig = new SlackConfiguration();
            config.GetSection("Slack").Bind(slackConfig);

            var client = new ValidationClient(githubConfig);
            var repositories = client.ValidateOrganization().Result;
            ReportToConsole(repositories);
            ReportToSlack(slackConfig, repositories).Wait();
            Console.WriteLine("Duration {0}", (DateTime.UtcNow - start).TotalSeconds);
        }

        private static void ReportToConsole(ValidationReport[] reports)
        {
            foreach (var report in reports)
            {
                Console.WriteLine(report.RepositoryName);
                foreach (var error in report.Results)
                {
                    Console.WriteLine("{0} {1}", error.RuleName, error.IsValid);
                }
            }
        }

        private static async Task ReportToSlack(SlackConfiguration config, ValidationReport[] reports)
        {
            var slackClient = new SlackClient(config);
            var response = await slackClient.SendMessageAsync(reports);
            var isValid = response.IsSuccessStatusCode ? "valid" : "invalid";
            Console.WriteLine($"Received {isValid} response.");
        }
    }
}
