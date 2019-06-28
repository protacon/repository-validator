using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.AzureFunctions.GitHubDto;
using ValidationLibrary.GitHub;

namespace ValidationLibrary.AzureFunctions
{
    public static class RepositoryValidator
    {
        private const string ProductHeader = "PTCS-Repository-Validator";

        [FunctionName("RepositoryValidator")]
        public static async Task Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, ILogger log, ExecutionContext context)
        {
            log.LogDebug("Repository validation hook launched");
            var content = await req.Content.ReadAsAsync<PushData>();
            ValidateInput(content);

            log.LogInformation("Repository {owner}/{repositoryName}", content.Repository?.Owner?.Login, content.Repository?.Name);

            log.LogDebug("Loading configuration.");
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var githubConfig = new GitHubConfiguration();
            config.GetSection("GitHub").Bind(githubConfig);
            
            ValidateConfig(githubConfig);

            log.LogDebug("Doing validation.");
            
            var ghClient = CreateClient(githubConfig);
            var client = new ValidationClient(log, ghClient);
            await client.Init();
            var repository = await client.ValidateRepository(content.Repository.Owner.Login, content.Repository.Name);

            log.LogDebug("Sending report.");
            await ReportToGitHub(log, ghClient, repository);
            log.LogInformation("Validation finished");
        }

        private static void ValidateInput(PushData content)
        {
            if (content == null)
            {
                throw new ArgumentNullException("Content was null. Unable to retrieve parameters.");
            }

            if (content.Repository == null)
            {
                throw new ArgumentException("No repository defined in content. Unable to validate repository");
            }

            if (content.Repository.Owner == null)
            {
                throw new ArgumentException("No repository owner defined. Unable to validate repository");
            }

            if (content.Repository.Name == null)
            {
                throw new ArgumentException("No repository name defined. Unable to validate repository");
            }
        }

        private static async Task ReportToGitHub(ILogger logger, GitHubClient client,  params ValidationReport[] reports)
        {
            var gitHubReportConfig = new GitHubReportConfig();
            gitHubReportConfig.GenericNotice = 
            "These issues are created, closed and reopened by [repository validator](https://github.com/protacon/repository-validator) when commits are pushed to repository. " + Environment.NewLine +
            Environment.NewLine +
            "If there are problems, please add an issue to [repository validator](https://github.com/protacon/repository-validator)" + Environment.NewLine +
            Environment.NewLine +
            "DO NOT change the name of this issue. Names are used to identify the issues created by automation." + Environment.NewLine;

            gitHubReportConfig.Prefix = "[Automatic validation]";

            var reporter = new GitHubReporter(logger, client, gitHubReportConfig);
            await reporter.Report(reports);
        }

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

        private static GitHubClient CreateClient(GitHubConfiguration gitHubConfiguration)
        {
            var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
            var tokenAuth = new Credentials(gitHubConfiguration.Token);
            client.Credentials = tokenAuth;
            return client;
        }
    }
}
