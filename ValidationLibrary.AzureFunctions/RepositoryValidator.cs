using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using ValidationLibrary.AzureFunctions.GitHubDto;
using ValidationLibrary.GitHub;

namespace ValidationLibrary.AzureFunctions
{
    public class RepositoryValidator
    {
        private readonly ILogger<RepositoryValidator> _logger;
        private readonly IGitHubClient _gitHubClient;
        private readonly IValidationClient _validationClient;

        public RepositoryValidator(ILogger<RepositoryValidator> logger, IGitHubClient gitHubClient, IValidationClient validationClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
            _validationClient = validationClient ?? throw new ArgumentNullException(nameof(validationClient));
        }

        [FunctionName("RepositoryValidator")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req)
        {
            try
            {
                _logger.LogDebug("Repository validation hook launched");
                if (req.Content == null)
                {
                    throw new ArgumentNullException("Request content was null. Unable to retrieve parameters.");
                }

                var content = await req.Content.ReadAsAsync<PushData>();
                ValidateInput(content);

                _logger.LogInformation("Doing validation. Repository {owner}/{repositoryName}", content.Repository?.Owner?.Login, content.Repository?.Name);

                await _validationClient.Init();
                var report = await _validationClient.ValidateRepository(content.Repository.Owner.Login, content.Repository.Name, false);

                _logger.LogDebug("Sending report.");
                await ReportToGitHub(report);
                await PerformAutofixes(report);
                _logger.LogInformation("Validation finished");
                return new OkResult();
            }
            catch (Exception exception)
            {
                if (exception is ArgumentException || exception is JsonException)
                {
                    _logger.LogError(exception, "Invalid request received");
                    return new BadRequestResult();
                }

                throw;
            }
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

        private async Task ReportToGitHub(params ValidationReport[] reports)
        {
            var gitHubReportConfig = new GitHubReportConfig
            {
                GenericNotice =
                    "These issues are created, closed and reopened by [repository validator](https://github.com/protacon/repository-validator) when commits are pushed to repository. " + Environment.NewLine +
                    Environment.NewLine +
                    "If there are problems, please add an issue to [repository validator](https://github.com/protacon/repository-validator)" + Environment.NewLine +
                    Environment.NewLine +
                    "DO NOT change the name of this issue. Names are used to identify the issues created by automation." + Environment.NewLine,
                Prefix = "[Automatic validation]"
            };

            var reporter = new GitHubReporter(_logger, _gitHubClient, gitHubReportConfig);
            await reporter.Report(reports);
        }

        private async Task PerformAutofixes(params ValidationReport[] results)
        {
            foreach (var repositoryResult in results)
            {
                foreach (var ruleResult in repositoryResult.Results.Where(r => !r.IsValid))
                {
                    await ruleResult.Fix(_gitHubClient, repositoryResult.Repository);
                }
            }
        }
    }
}
