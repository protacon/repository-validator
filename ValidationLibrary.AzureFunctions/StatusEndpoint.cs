using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

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
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequestMessage req)
        {
            _logger.LogDebug("Repository validator status check hook launched.");

            return new JsonResult(new
            {
                Rules = _validator.GetRules().Select(r => r.GetConfiguration()).ToArray()
            });
        }
    }
}