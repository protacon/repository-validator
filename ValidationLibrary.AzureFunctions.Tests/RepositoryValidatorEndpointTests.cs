using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.AzureFunctions.GitHubDto;
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
            var request = new PushData();
            var result = await _repositoryValidator.RunActivity(request) as BadRequestResult;
            Assert.NotNull(result, "The repository validator run result was not a BadRequestResult as expected.");
            Assert.AreEqual((int)HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task Run_ReturnsBadRequestForIncorrectJsonToPushData()
        {
            var report = new ValidationReport
            {
                Results = new ValidationResult[0]
            };

            var content = new PushData
            {
                Repository = new GitHubDto.Repository
                {
                    Name = "repository-validator-testing",
                    Owner = new Owner
                    {
                        Login = ""
                    }
                }
            };

            _mockValidationClient.ValidateRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(report);
            var result = await _repositoryValidator.RunActivity(content) as BadRequestResult;
            Assert.NotNull(result, "The repository validator run result was not a BadRequestResult as expected.");
            Assert.AreEqual((int)HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task Run_ValidatesCorrectRepository()
        {
            var report = new ValidationReport
            {
                Results = new ValidationResult[0]
            };

            var content = new PushData
            {
                Repository = new GitHubDto.Repository
                {
                    Name = "repository-validator-testing",
                    Owner = new Owner
                    {
                        Login = "protacon"
                    }
                }
            };
            _mockValidationClient.ValidateRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(report);
            var result = await _repositoryValidator.RunActivity(content) as OkResult;
            Assert.NotNull(result, "The repository validator run result was not an OkResult as expected.");
            await _mockValidationClient.Received().ValidateRepository(content.Repository.Owner.Login, content.Repository.Name, false);
        }
    }
}
