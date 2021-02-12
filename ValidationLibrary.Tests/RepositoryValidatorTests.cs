using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;

namespace ValidationLibrary.Tests
{
    [TestFixture]
    public class RepositoryValidatorTests
    {
        private const string ConfigFileName = "repository-validator.json";

        private RepositoryValidator _repositoryValidator;

        private IValidationRule[] _mockRules;
        private IGitHubClient _mockClient;
        private IRepositoryContentsClient _mockRepositoryContentsClient;

        [SetUp]
        public void SetUp()
        {
            var mockLogger = Substitute.For<ILogger<RepositoryValidator>>();

            _mockClient = Substitute.For<IGitHubClient>();

            _mockRules = new IValidationRule[]
            {
                Substitute.For<IValidationRule>(),
                Substitute.For<IValidationRule>(),
                Substitute.For<IValidationRule>()
            };

            _repositoryValidator = new RepositoryValidator(mockLogger, _mockClient, _mockRules);

            _mockRepositoryContentsClient = Substitute.For<IRepositoryContentsClient>();
            _mockClient.Repository.Content.Returns(_mockRepositoryContentsClient);
        }

        [Test]
        public async Task Init_InitiatesAllRules()
        {
            await _repositoryValidator.Init();

            foreach (var mockRule in _mockRules)
            {
                await mockRule.Received().Init(_mockClient);
            }
        }

        [Test]
        public async Task Validate_ValidationWithAllRulesIfNoneAreIgnoredAndConfigIsNotFound()
        {
            var repository = CreateRepository("testOwner", "mock-repository", true, false);
            _mockRepositoryContentsClient
                .GetAllContents("testOwner", "mock-repository", ConfigFileName)
                .Returns<Task<IReadOnlyList<RepositoryContent>>>(x => { throw new NotFoundException("no message", HttpStatusCode.NotFound); });
            foreach (var rule in _mockRules)
            {
                var singleRuleValidationResult = new ValidationResult($"Rule {Guid.NewGuid()}", "With vasara", false, (c, r) => Task.CompletedTask);
                rule.IsValid(_mockClient, repository).Returns(Task.FromResult(singleRuleValidationResult));
            }

            var result = await _repositoryValidator.Validate(repository, false);

            Assert.NotNull(result);
            Assert.AreEqual(3, result.Results.Length);
            foreach (var resultItem in result.Results)
            {
                Assert.NotNull(resultItem);
            }
            foreach (var mockRule in _mockRules)
            {
                await mockRule.Received().IsValid(_mockClient, repository);
            }
        }

        private Repository CreateRepository(string owner, string name, bool hasIssues, bool archived)
        {
            var user = new User(null, null, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, 0, null, 0, 0, false, null, 0, 0, null, owner, null, null, 0, null, 0, 0, 0, null, new RepositoryPermissions(), false, null, null);
            return new Repository(null, null, null, null, null, null, null, 0, null, user, name, $"{owner}/{name}", false, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, hasIssues, false, false, false, 0, 0, null, null, null, archived, 0);
        }
    }
}
