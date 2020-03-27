using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary.AzureFunctions.GitHubDto;
using ValidationLibrary.GitHub;

namespace ValidationLibrary.AzureFunctions
{
    public class RepositoryValidatorEndpoint
    {
        private readonly IGitHubClient _gitHubClient;
        private readonly IValidationClient _validationClient;
        private readonly IGitHubReporter _gitHubReporter;

        public RepositoryValidatorEndpoint(IGitHubClient gitHubClient, IValidationClient validationClient, IGitHubReporter gitHubReporter)
        {
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
            _validationClient = validationClient ?? throw new ArgumentNullException(nameof(validationClient));
            _gitHubReporter = gitHubReporter ?? throw new ArgumentNullException(nameof(gitHubReporter));
        }

        [FunctionName(nameof(RepositoryValidatorTrigger))]
        public static async Task<HttpResponseMessage> RepositoryValidatorTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, [DurableClient] IDurableOrchestrationClient starter,
            ILogger logger)
        {
            logger.LogDebug("Repository validation hook launched.");
            if (starter is null) throw new ArgumentNullException(nameof(starter), "Durable orchestration client was null. Error using durable functions.");
            if (req == null || req.Content == null) throw new ArgumentNullException(nameof(req), "Request content was null. Unable to retrieve parameters.");

            var content = await req.Content.ReadAsAsync<PushData>().ConfigureAwait(false);
            logger.LogDebug("Request json valid.");
            var instanceId = await starter.StartNewAsync(nameof(RunOrchestrator), content).ConfigureAwait(false);

            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(RunOrchestrator))]
        public static async Task<StatusCodeResult> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context), "Durable orchestration context was null. Error running the orchestrator.");

            var content = context.GetInput<PushData>();
            return await context.CallActivityAsync<StatusCodeResult>(nameof(RunActivity), content).ConfigureAwait(true);
        }

        [FunctionName(nameof(RunActivity))]
        public async Task<StatusCodeResult> RunActivity([ActivityTrigger] PushData content, ILogger logger)
        {
            try
            {
                logger.LogDebug("Executing validation activity.");
                if (content is null) throw new ArgumentNullException(nameof(content), "No content to execute the activity.");

                ValidateInput(content);

                logger.LogInformation("Doing validation. Repository {owner}/{repositoryName}", content.Repository?.Owner?.Login, content.Repository?.Name);

                await _validationClient.Init().ConfigureAwait(false);
                var report = await _validationClient.ValidateRepository(content.Repository.Owner.Login, content.Repository.Name, false).ConfigureAwait(false);

                logger.LogDebug("Sending report.");
                await _gitHubReporter.Report(new[] { report }).ConfigureAwait(false);

                logger.LogDebug("Performing auto fixes.");
                await PerformAutofixes(report).ConfigureAwait(false);

                logger.LogInformation("Validation finished");
                return new OkResult();
            }
            catch (Exception exception)
            {
                if (exception is ArgumentException)
                {
                    logger.LogError(exception, "Invalid request received");

                    return new BadRequestResult();
                }
                throw;
            }
        }

        private static void ValidateInput(PushData content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content), "Content was null. Unable to retrieve parameters.");
            }

            if (content.Repository is null)
            {
                throw new ArgumentException("No repository defined in content. Unable to validate repository.");
            }

            if (content.Repository.Owner is null)
            {
                throw new ArgumentException("No repository owner defined. Unable to validate repository.");
            }

            if (string.IsNullOrEmpty(content.Repository.Name))
            {
                throw new ArgumentException("No repository name defined. Unable to validate repository.");
            }

            if (string.IsNullOrEmpty(content.Repository.Owner.Login))
            {
                throw new ArgumentException("No repository owner login defined. Unable to validate repository.");
            }
        }
        private async Task PerformAutofixes(params ValidationReport[] results)
        {
            foreach (var repositoryResult in results)
            {
                foreach (var ruleResult in repositoryResult.Results.Where(r => !r.IsValid))
                {
                    await ruleResult.Fix(_gitHubClient, repositoryResult.Repository).ConfigureAwait(false);
                }
            }
        }
    }
}
