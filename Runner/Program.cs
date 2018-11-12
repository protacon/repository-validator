using System;
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
            Report(slackConfig, repositories).Wait();
        }

        private static async Task Report(SlackConfiguration config, ValidationReport[] reports)
        {
            var slackClient = new SlackClient(config);
            var response = await slackClient.SendMessageAsync(reports);
            var isValid = response.IsSuccessStatusCode ? "valid" : "invalid";
            Console.WriteLine($"Received {isValid} response.");
        }
    }
}
