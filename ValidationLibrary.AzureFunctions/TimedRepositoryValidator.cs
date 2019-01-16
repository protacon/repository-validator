using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ValidationLibrary.AzureFunctions.GitHubDto;
using ValidationLibrary.Slack;

namespace ValidationLibrary.AzureFunctions
{
    public static class TimedRepositoryValidator
    {
        [FunctionName("TimedRepositoryValidator")]
        public static async Task Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, ILogger log, ExecutionContext context)
        {
            log.LogDebug("Repository validation hook launched");

            var content = await req.Content.ReadAsAsync<PushData>();

            log.LogInformation("{0} {1}", content.Organization, content.Repository);

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
            var repository = await client.ValidateRepository(content.Organization.Name, content.Repository.Name);

            log.LogDebug("Sending report.");
            await Report(slackConfig, repository);

            log.LogInformation("Validation finished");
        }

        private static async Task Report(SlackConfiguration config, ValidationReport report)
        {
            var slackClient = new SlackClient(config);
            var response = await slackClient.SendMessageAsync(report);
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
