using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Rules;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Tests.Rules
{
    public class HasReadmeRuleTests
    {
        /// <summary>
        /// By default, master is checked for Jenkinsfile if there is no branch
        /// </summary>
        private const string MasterBranch = "master";

        private HasReadmeRule _rule;

        private User _owner;

        private IGitHubClient _mockClient;
        private IRepositoriesClient _mockRepositoryClient;
        private IRepositoryContentsClient _mockRepositoryContentClient;

        [SetUp]
        public async Task Setup()
        {
            _owner = new User(null, null, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, 0, null, 0, 0, false, null, 0, 0, null, "protacon", "protacon", null, 0, null, 0, 0, 0, null, new RepositoryPermissions(), false, null, null);

            _mockClient = Substitute.For<IGitHubClient>();
            _mockRepositoryClient = Substitute.For<IRepositoriesClient>();
            _mockClient.Repository.Returns(_mockRepositoryClient);
            _mockRepositoryContentClient = Substitute.For<IRepositoryContentsClient>();
            _mockRepositoryClient.Content.Returns(_mockRepositoryContentClient);

            _rule = new HasReadmeRule(
                Substitute.For<ILogger<HasReadmeRule>>(),
                new GitUtils(Substitute.For<ILogger<GitUtils>>()));

            var mockReleaseClient = Substitute.For<IReleasesClient>();
            _mockRepositoryClient.Release.Returns(mockReleaseClient);
            await _rule.Init(_mockClient);
        }

        [Test]
        public void RuleName_IsDefined()
        {
            Assert.NotNull(_rule.RuleName);
        }

        [Test]
        public async Task IsValid_ReturnsOkWhenReadmeExists()
        {
            var readme = CreateReadme("README.md", "random content");
            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetReadme(_owner.Name, repository.Name).Returns(Task.FromResult(readme));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenReadmeExistsWithoutContent()
        {
            var readme = CreateReadme("README.md", "");
            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetReadme(_owner.Name, repository.Name).Returns(Task.FromResult(readme));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenReadmeDoesNotExist()
        {
            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetReadme(_owner.Name, repository.Name).Returns(Task.FromResult<Readme>(null));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        private Readme CreateReadme(string name, string content)
        {
            return new Readme(null, content, name, null, null);
        }

        private Repository CreateRepository(string name)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, _owner, name, null, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false);
        }
    }
}