using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Octokit;

namespace ValidationLibrary.AzureFunctions.Tests
{
    [TestFixture]
    public class RepositoryValidatorEndpointTests
    {
        private RepositoryValidatorEndpoint _repositoryValidator;

        [SetUp]
        public void Setup()
        {
            _repositoryValidator = new RepositoryValidatorEndpoint(Substitute.For<ILogger<RepositoryValidatorEndpoint>>(), Substitute.For<IGitHubClient>(), Substitute.For<IValidationClient>());
        }

        [Test]
        public async Task Run_ReturnsBadRequestForMissingContent()
        {
            var request = new HttpRequestMessage();
            var result = await _repositoryValidator.Run(request);
            var casted = result as BadRequestResult;
            Assert.NotNull(casted, "The repository validator run result was not a BadRequestResult as expected.");
            Assert.AreEqual((int)HttpStatusCode.BadRequest, casted.StatusCode);
        }

        [Test]
        public async Task Run_ReturnsBadRequestForInvalidJson()
        {
            var dynamic = new
            {
                repository = new
                {
                    name = "repository-validator-testing",
                    owner = "protacon"
                }
            };

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(dynamic), System.Text.Encoding.UTF8, "application/json"),
            };
            var result = await _repositoryValidator.Run(request);
            var casted = result as BadRequestResult;
            Assert.NotNull(casted, "The repository validator run result was not a BadRequestResult as expected.");
            Assert.AreEqual((int)HttpStatusCode.BadRequest, casted.StatusCode);
        }
    }
}