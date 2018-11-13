using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ValidationLibrary.Slack;

namespace ValidationLibrary.AzureFunctions
{
    public static class TimedRepositoryValidator
    {
        [FunctionName("TimedRepositoryValidator")]
        public static async Task Run([TimerTrigger("0 0 0 1 1 *")]TimerInfo timer, ILogger log, ExecutionContext context)
        {
            log.LogDebug("Loading configuration.");
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            
            var githubConfig = new GitHubConfiguration();
            config.GetSection("GitHub").Bind(githubConfig);

            var slackConfig = new SlackConfiguration();
            config.GetSection("Slack").Bind(slackConfig);

            log.LogDebug("Doing validation.");
            var client = new ValidationClient(githubConfig);
            var repositories = await client.ValidateOrganization();

            log.LogDebug("Sending report.");
            await Report(slackConfig, repositories);

            log.LogInformation("Validation finished");
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
