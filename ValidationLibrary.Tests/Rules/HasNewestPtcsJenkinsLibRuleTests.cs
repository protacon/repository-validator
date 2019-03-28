using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary.Tests.Rules
{
    public class HasNewestPtcsJenkinsLibRuleTests
    {
        private const string NewestJenkinsPtcsLibrary = "0.3.2";

        private HasNewestPtcsJenkinsLibRule _rule;

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

            var logger = Substitute.For<ILogger>();

            _rule = new HasNewestPtcsJenkinsLibRule(logger);
            
            var mockReleaseClient = Substitute.For<IReleasesClient>();
            _mockRepositoryClient.Release.Returns(mockReleaseClient);
            var release = new Release(null, null, null, null, 0, null, NewestJenkinsPtcsLibrary, null, null,null, false, false, DateTime.UtcNow, null, null, null, null, null);
            _mockRepositoryClient.Release.GetLatest("protacon", "jenkins-ptcs-library").Returns(Task.FromResult(release));
            await _rule.Init(_mockClient);
        }

        [Test]
        public void RuleName_IsDefined()
        {
            Assert.NotNull(_rule.RuleName);
        }

        [Test]
        public async Task IsValid_ReturnsOkIFThereIsNoJenkinsFile()
        {
            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetAllContents(_owner.Name, repository.Name).Returns(Task.FromResult((IReadOnlyList<RepositoryContent>)new List<RepositoryContent>()));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsOkIfJenkinsFileHasNoPtcsLibrary()
        {
            var content = CreateContent("JENKINSFILE", "random content");
            IReadOnlyList<RepositoryContent> contents = new []{content};

            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetAllContents(_owner.Name, repository.Name).Returns(Task.FromResult(contents));
            _mockRepositoryContentClient.GetAllContents(_owner.Name, repository.Name, contents[0].Name).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenPtcsLibraryIsOld()
        {
            var content = CreateContent("JENKINSFILE", "library 'jenkins-ptcs-library@0.3.0'");
            IReadOnlyList<RepositoryContent> contents = new []{content};

            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetAllContents(_owner.Name, repository.Name).Returns(Task.FromResult(contents));
            _mockRepositoryContentClient.GetAllContents(_owner.Name, repository.Name, contents[0].Name).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsTrueWhenPtcsLibraryIsNewest()
        {
            var content = CreateContent("JENKINSFILE", $"library 'jenkins-ptcs-library@{NewestJenkinsPtcsLibrary}'");
            IReadOnlyList<RepositoryContent> contents = new []{content};

            var repository = CreateRepository("repomen");
            _mockRepositoryContentClient.GetAllContents(_owner.Name, repository.Name).Returns(Task.FromResult(contents));
            _mockRepositoryContentClient.GetAllContents(_owner.Name, repository.Name, contents[0].Name).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(_mockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        private RepositoryContent CreateContent(string name, string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var converted = System.Convert.ToBase64String(bytes);
            return new RepositoryContent(name, null, null, 0, ContentType.File, null, null, null, null, null, converted, null, null);
        }

        private Repository CreateRepository(string name)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, _owner, name, null, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false);
        }
    }
}