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
        /// By default, master is checked for README.md if there is no branch
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

        static string[] ReadMeNames = { "ReAdMe.md", "rEaDmE.txt", "README", "readme", "ReAdMe.doc" };

        [TestCaseSource("ReadMeNames")]
        public async Task IsValid_ReturnsOkWhenReadmeExists(string readMeName)
        {
            var readme = CreateContent(readMeName, "random content");
            IReadOnlyList<RepositoryContent> contents = new[] { readme };
            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetAllContentsByRef(_owner.Name, repository.Name, contents[0].Name, MasterBranch).Returns(Task.FromResult(contents));
            _mockRepositoryContentClient.GetAllContentsByRef(_owner.Name, repository.Name, MasterBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenReadmeExistsWithoutContent()
        {
            var readme = CreateContent("README.md", "");
            IReadOnlyList<RepositoryContent> contents = new[] { readme };
            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetAllContentsByRef(_owner.Name, repository.Name, contents[0].Name, MasterBranch).Returns(Task.FromResult(contents));
            _mockRepositoryContentClient.GetAllContentsByRef(_owner.Name, repository.Name, MasterBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenReadmeDoesNotExist()
        {
            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetAllContentsByRef(_owner.Name, repository.Name, MasterBranch).Returns(Task.FromResult((IReadOnlyList<RepositoryContent>)new List<RepositoryContent>()));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        private RepositoryContent CreateContent(string name, string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var converted = Convert.ToBase64String(bytes);
            return new RepositoryContent(name, null, null, 0, ContentType.File, null, null, null, null, null, converted, null, null);
        }

        private Repository CreateRepository(string name)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, _owner, name, null, false, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false);
        }
    }
}