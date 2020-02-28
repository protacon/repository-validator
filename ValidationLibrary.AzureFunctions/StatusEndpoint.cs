using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ValidationLibrary.AzureFunctions
{
    public class StatusEndpoint
    {
        private readonly ILogger<StatusEndpoint> _logger;
        private ValidationLibrary.RepositoryValidator _validator;
        public StatusEndpoint(ILogger<StatusEndpoint> logger, ValidationLibrary.RepositoryValidator validator)
        {
            _logger = logger;
            _validator = validator;
        }

        [FunctionName("StatusCheck")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req)
        {
            try
            {
                _logger.LogDebug("Repository validator status check hook launched.");

                return new JsonResult(new
                {
                    Rules = _validator.GetRules().Select(r => r.GetConfiguration())
                });
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
    }
}