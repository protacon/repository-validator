using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.AzureFunctions.GitHubDto;
using ValidationLibrary.GitHub;

namespace ValidationLibrary.AzureFunctions
{
    public class RepositoryValidator
    {
        private readonly ILogger<RepositoryValidator> _logger;
        private readonly IGitHubClient _gitHubClient;
        private readonly ValidationClient _validationClient;

        public RepositoryValidator(ILogger<RepositoryValidator> logger, IGitHubClient gitHubClient, ValidationClient validationClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
            _validationClient = validationClient ?? throw new ArgumentNullException(nameof(validationClient));
        }

        [FunctionName("RepositoryValidator")]
        public async Task Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, ExecutionContext context)
        {
            _logger.LogDebug("Repository validation hook launched");
            var content = await req.Content.ReadAsAsync<PushData>();
            ValidateInput(content);

            _logger.LogInformation("Doing validation. Repository {owner}/{repositoryName}", content.Repository?.Owner?.Login, content.Repository?.Name);
            
            await _validationClient.Init();
            var repository = await _validationClient.ValidateRepository(content.Repository.Owner.Login, content.Repository.Name);

            _logger.LogDebug("Sending report.");
            await ReportToGitHub(_logger, repository);
            _logger.LogInformation("Validation finished");
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

        private async Task ReportToGitHub(ILogger logger, params ValidationReport[] reports)
        {
            var gitHubReportConfig = new GitHubReportConfig();
            gitHubReportConfig.GenericNotice = 
            "These issues are created, closed and reopened by [repository validator](https://github.com/protacon/repository-validator) when commits are pushed to repository. " + Environment.NewLine +
            Environment.NewLine +
            "If there are problems, please add an issue to [repository validator](https://github.com/protacon/repository-validator)" + Environment.NewLine +
            Environment.NewLine +
            "DO NOT change the name of this issue. Names are used to identify the issues created by automation." + Environment.NewLine;

            gitHubReportConfig.Prefix = "[Automatic validation]";

            var reporter = new GitHubReporter(logger, _gitHubClient, gitHubReportConfig);
            await reporter.Report(reports);
        }
    }
}
