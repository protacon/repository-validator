using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
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
        private IDurableOrchestrationClient _mockDurableClient;

        [SetUp]
        public void Setup()
        {
            _mockGitHubClient = Substitute.For<IGitHubClient>();
            _mockValidationClient = Substitute.For<IValidationClient>();
            _mockGitHubReporter = Substitute.For<IGitHubReporter>();
            _repositoryValidator = new RepositoryValidatorEndpoint(_mockGitHubClient, _mockValidationClient, _mockGitHubReporter);
            _mockDurableClient = Substitute.For<IDurableOrchestrationClient>();
        }

        [Test]
        public async Task RunActivity_ValidatesCorrectRepository()
        {
            var report = new ValidationReport
            {
                Results = new ValidationResult[0]
            };
            _mockValidationClient.ValidateRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(report);

            var content = new PushData
            {
                Repository = new GitHubDto.Repository
                {
                    Name = "repository-validator-testing",
                    Owner = new Owner
                    {
                        Login = "by-pinja"
                    }
                }
            };

            var result = await _repositoryValidator.RunActivity(content, Substitute.For<ILogger>()) as OkResult;

            Assert.NotNull(result, "The repository validator run result was not an OkResult as expected.");
            Assert.AreEqual((int)HttpStatusCode.OK, result.StatusCode);
            await _mockValidationClient.Received().ValidateRepository(content.Repository.Owner.Login, content.Repository.Name, false);
        }

        [Test]
        public async Task RunActivity_ReturnsBadRequestForIncorrectJsonToPushData()
        {
            var report = new ValidationReport
            {
                Results = new ValidationResult[0]
            };
            _mockValidationClient.ValidateRepository(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(report);

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

            var result = await _repositoryValidator.RunActivity(content, Substitute.For<ILogger>()) as BadRequestResult;

            Assert.NotNull(result, "The repository validator run result was not a BadRequestResult as expected.");
            Assert.AreEqual((int)HttpStatusCode.BadRequest, result.StatusCode);
            await _mockValidationClient.DidNotReceive().ValidateRepository(content.Repository.Owner.Login, content.Repository.Name, false);
        }

        [Test]
        public async Task RunActivity_ValidatesTrigger()
        {
            const string InstanceId = "by-pinja_repository-validator-testing";
            _mockDurableClient.StartNewAsync(Arg.Any<string>(), Arg.Any<object>()).Returns(Task.FromResult(InstanceId));
            _mockDurableClient.GetStatusAsync(Arg.Any<string>()).Returns(Task.FromResult<DurableOrchestrationStatus>(null));

            _mockDurableClient.CreateCheckStatusResponse(Arg.Any<HttpRequestMessage>(), InstanceId).Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty)
            });

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

            var result = await RepositoryValidatorEndpoint.RepositoryValidatorTrigger(request, _mockDurableClient, Substitute.For<ILogger>());

            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
            await _mockDurableClient.Received().StartNewAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
            _mockDurableClient.Received().CreateCheckStatusResponse(Arg.Any<HttpRequestMessage>(), InstanceId);
        }

        [Test]
        public async Task RunActivity_InvalidJsonThrowsError()
        {
            var dynamic = new
            {
                repository = new
                {
                    name = "repository-validator-testing",
                    owner = "test"
                }
            };

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(dynamic), System.Text.Encoding.UTF8, "application/json"),
            };

            var result = await RepositoryValidatorEndpoint.RepositoryValidatorTrigger(request, _mockDurableClient, Substitute.For<ILogger>());
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            await _mockDurableClient.DidNotReceive().StartNewAsync(Arg.Any<string>(), Arg.Any<object>());
            _mockDurableClient.DidNotReceive().CreateCheckStatusResponse(Arg.Any<HttpRequestMessage>(), Arg.Any<string>());
        }

        [Test]
        public async Task RunActivity_DoesntStartNewValidationIfExistingIsRunning()
        {
            var data = CreatePushData("test", "test");
            var instanceId = CreateInstanceId(data);
            _mockDurableClient.CreateCheckStatusResponse(Arg.Any<HttpRequestMessage>(), instanceId).Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty)
            });

            var status = new DurableOrchestrationStatus
            {
                RuntimeStatus = OrchestrationRuntimeStatus.Running
            };
            _mockDurableClient.GetStatusAsync(instanceId).Returns(Task.FromResult(status));

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json"),
            };

            var result = await RepositoryValidatorEndpoint.RepositoryValidatorTrigger(request, _mockDurableClient, Substitute.For<ILogger>());

            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
            await _mockDurableClient.DidNotReceiveWithAnyArgs().StartNewAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
        }

        [Test]
        public async Task RunActivity_RetriesForCompleted(
            [Values(OrchestrationRuntimeStatus.Canceled, OrchestrationRuntimeStatus.Failed, OrchestrationRuntimeStatus.Terminated, OrchestrationRuntimeStatus.Completed)] OrchestrationRuntimeStatus runtimeStatus)
        {
            var data = CreatePushData("test", "test");
            var instanceId = CreateInstanceId(data);
            _mockDurableClient.CreateCheckStatusResponse(Arg.Any<HttpRequestMessage>(), instanceId).Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty)
            });

            var status = new DurableOrchestrationStatus
            {
                RuntimeStatus = runtimeStatus
            };
            _mockDurableClient.GetStatusAsync(instanceId).Returns(Task.FromResult(status));

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json"),
            };

            var result = await RepositoryValidatorEndpoint.RepositoryValidatorTrigger(request, _mockDurableClient, Substitute.For<ILogger>());

            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
            await _mockDurableClient.Received().StartNewAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
        }

        private PushData CreatePushData(string organization, string name)
        {
            return new PushData
            {
                Repository = new GitHubDto.Repository
                {
                    Name = name,
                    Owner = new Owner
                    {
                        Login = organization
                    }
                }
            };
        }

        private static string CreateInstanceId(PushData content)
        {
            return $"{content.Repository?.Owner?.Login}_{content.Repository?.Name}";
        }
    }
}
