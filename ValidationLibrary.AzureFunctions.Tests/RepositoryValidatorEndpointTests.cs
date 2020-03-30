using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.GitHub;

namespace ValidationLibrary.AzureFunctions.Tests
{
    [TestFixture]
    public class RepositoryValidatorEndpointTests
    {
        private IGitHubClient _mockGitHubClient;
        private IValidationClient _mockValidationClient;
        private IGitHubReporter _mockGitHubReporter;

        private RepositoryValidatorEndpoint _repositoryValidator;

        [SetUp]
        public void Setup()
        {
            _mockGitHubClient = Substitute.For<IGitHubClient>();
            _mockValidationClient = Substitute.For<IValidationClient>();
            _mockGitHubReporter = Substitute.For<IGitHubReporter>();
            _repositoryValidator = new RepositoryValidatorEndpoint(Substitute.For<ILogger<RepositoryValidatorEndpoint>>(), _mockGitHubClient, _mockValidationClient, _mockGitHubReporter);
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
                    owner = "by-pinja"
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

        [Test]
        public async Task Run_ValidatesCorrectRepository()
        {
            var report = new ValidationReport
            {
                Results = new ValidationResult[0]
            };

            var dynamic = new
            {
                repository = new
                {
                    name = "repository-validator-testing",
                    owner = new
                    {
                        login = "by-pinja"
                    }
                }
            };

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(dynamic), System.Text.Encoding.UTF8, "application/json"),
            };
            _mockValidationClient.ValidateRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(report);
            var result = await _repositoryValidator.Run(request) as OkResult;
            Assert.NotNull(result);
            await _mockValidationClient.Received().ValidateRepository(dynamic.repository.owner.login, dynamic.repository.name, false);
        }
    }
}
