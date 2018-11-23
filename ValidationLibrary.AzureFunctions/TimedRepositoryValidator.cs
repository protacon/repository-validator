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
            log.LogInformation("Starting repository validator.");

            log.LogDebug("Loading configuration.");
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            
            var githubConfig = new GitHubConfiguration();
            config.GetSection("GitHub").Bind(githubConfig);

            var slackConfig = new SlackConfiguration();
            config.GetSection("Slack").Bind(slackConfig);

            ValidateConfig(githubConfig, slackConfig);

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
        }

        private static void ValidateConfig(GitHubConfiguration gitHubConfiguration, SlackConfiguration slackConfiguration)
        {
            if (gitHubConfiguration.Organization == null)
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Organization), "Organization was missing.");
            }

            if (gitHubConfiguration.Token == null)
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Token), "Token was missing.");
            }

            if (slackConfiguration.WebHookUrl == null)
            {
                throw new ArgumentNullException(nameof(slackConfiguration.WebHookUrl), "WebHookUrl was missing.");
            }
        }
    }
}
