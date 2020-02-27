using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ValidationLibrary.AzureFunctions.GitHubDto;

namespace ValidationLibrary.AzureFunctions
{
    public class StatusEndpoint
    {
        private readonly ILogger<StatusEndpoint> _logger;
        public StatusEndpoint(ILogger<StatusEndpoint> logger)
        {
            _logger = logger;
        }

        [FunctionName("StatusCheck")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req)
        {
            try
            {
                _logger.LogDebug("Test Webhook hook launched");
                if (req == null || req.Content == null)
                {
                    throw new ArgumentNullException(nameof(req), "Request content was null. Unable to retrieve parameters.");
                }

                var content = await req.Content.ReadAsAsync<PushData>().ConfigureAwait(false);
                //ValidateInput(content);

                _logger.LogInformation("Testin point reached Yayyyyyyyyyyyyy");
                //_logger.LogInformation("Doing validation. Repository {owner}/{repositoryName}", content.Repository?.Owner?.Login, content.Repository?.Name);

                return new OkResult();
            }
            catch (Exception exception)
            {
                if (exception is ArgumentException || exception is JsonException)
                {
                    //_logger.LogError(exception, "Invalid request received");

                    return new BadRequestResult();
                }
                throw;
            }
        }
    }
}