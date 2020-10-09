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
    public class HasReadmeRuleTests : BaseRuleTests<HasReadmeRule>
    {
        protected override void OnSetup()
        {
            _rule = new HasReadmeRule(
                Substitute.For<ILogger<HasReadmeRule>>(),
                new GitUtils(Substitute.For<ILogger<GitUtils>>()));

            var mockReleaseClient = Substitute.For<IReleasesClient>();
            MockRepositoryClient.Release.Returns(mockReleaseClient);
        }

        [Test]
        public async Task IsValid_ReturnsOkWhenReadmeExists([Values("ReAdMe.md", "rEaDmE.txt", "README", "readme", "ReAdMe.doc")] string readMeName)
        {
            var readme = CreateContent(readMeName, "random content");
            IReadOnlyList<RepositoryContent> contents = new[] { readme };
            var repository = CreateRepository("repomen");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenReadmeDoesNotExist([Values("aREADME.md", "rEaDmE.txta", "README.", "readm", "ReAdMea.doc", "")] string readMeName)
        {
            var contents = new List<RepositoryContent>();
            if (!string.IsNullOrEmpty(readMeName))
            {
                var readme = CreateContent(readMeName, "random content");
                contents.Add(readme);
            }
            var repository = CreateRepository("repomen");
            if (contents.Count != 0)
            {
                MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult((IReadOnlyList<RepositoryContent>)contents));
            }
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult((IReadOnlyList<RepositoryContent>)contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseWhenReadmeExistsWithoutContent()
        {
            var readme = CreateContent("README.md", "");
            IReadOnlyList<RepositoryContent> contents = new[] { readme };
            var repository = CreateRepository("repomen");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));

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
            return new Repository(null, null, null, null, null, null, null, 0, null, Owner, name, null, false, null, null, null, false, false, 0, 0, MainBranch, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false, 0);
        }
    }
}
