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
    public class HasNewestPtcsJenkinsLibRuleTests : BaseRuleTests<HasNewestPtcsJenkinsLibRule>
    {
        private const string ExpectedJenkinsPtcsLibrary = "0.3.2";

        protected override void OnSetup()
        {
            _rule = new HasNewestPtcsJenkinsLibRule(
                Substitute.For<ILogger<HasNewestPtcsJenkinsLibRule>>(),
                new GitUtils(Substitute.For<ILogger<GitUtils>>()));

            var mockReleaseClient = Substitute.For<IReleasesClient>();
            MockRepositoryClient.Release.Returns(mockReleaseClient);
            var release = new Release(null, null, null, null, 0, null, ExpectedJenkinsPtcsLibrary, null, null, null, false, false, DateTime.UtcNow, null, null, null, null, null);
            MockRepositoryClient.Release.GetLatest("by-pinja", "jenkins-ptcs-library").Returns(Task.FromResult(release));
        }

        [Test]
        public async Task IsValid_ReturnsOkIfThereIsNoJenkinsFile()
        {
            var repository = CreateRepository("repomen");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult((IReadOnlyList<RepositoryContent>)new List<RepositoryContent>()));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsOkIfJenkinsFileHasNoPtcsLibrary()
        {
            var content = CreateContent("JENKINSFILE", "random content");
            IReadOnlyList<RepositoryContent> contents = new[] { content };

            var repository = CreateRepository("repomen");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenPtcsLibraryIsOldWithSingleQuotes()
        {
            var content = CreateContent("JENKINSFILE", "library 'jenkins-ptcs-library@0.3.0'");
            IReadOnlyList<RepositoryContent> contents = new[] { content };

            var repository = CreateRepository("repomen");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenPtcsLibraryIsOldWithDoubleQuotest()
        {
            var content = CreateContent("JENKINSFILE", "library \"jenkins-ptcs-library@0.3.0\"");
            IReadOnlyList<RepositoryContent> contents = new[] { content };

            var repository = CreateRepository("repomen");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsTrueWhenPtcsLibraryIsNewest()
        {
            var content = CreateContent("JENKINSFILE", $"library 'jenkins-ptcs-library@{ExpectedJenkinsPtcsLibrary}'");
            IReadOnlyList<RepositoryContent> contents = new[] { content };

            var repository = CreateRepository("repomen");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_IgnoresCommentedLibraryVersionButNotActual()
        {
            var content = CreateContent("JENKINSFILE", "//Actual version may be library 'jenkins-ptcs-library@0.3.0'" + Environment.NewLine + $"library 'jenkins-ptcs-library@0.3.0'");

            IReadOnlyList<RepositoryContent> contents = new[] { content };
            var repository = CreateRepository("repo");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
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
            return new Repository(null, null, null, null, null, null, null, 0, null, Owner, name, null, false, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false);
        }
    }
}
