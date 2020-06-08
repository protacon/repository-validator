using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary.Tests.Rules
{
    public class HasCodeownersRuleTests : BaseRuleTests<HasCodeownersRule>
    {
        protected override void OnSetup()
        {
            _rule = new HasCodeownersRule(Substitute.For<ILogger<HasCodeownersRule>>());
        }

        [Test]
        public async Task IsValid_ReturnsFalseIfNoCodeownersFile()
        {
            var repository = CreateRepository("repo");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch)
            .Returns(Task.FromResult((IReadOnlyList<RepositoryContent>)new List<RepositoryContent>()));

            var result = await _rule.IsValid(MockClient, repository);
            StringAssert.AreEqualIgnoringCase(result.HowToFix, "Add CODEOWNERS file.");
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsFalseIfCodeownersFileEmpty()
        {
            var content = CreateContent("CODEOWNERS", "");
            IReadOnlyList<RepositoryContent> contents = new[] { content };

            var repository = CreateRepository("repo");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            StringAssert.AreEqualIgnoringCase(result.HowToFix, "Add CODEOWNERS file & add at least one owner.");
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsTrueIfCodeownersFileExistsInGithubDirectoryAndHasAtleastOneEntry()
        {
            var directory = CreateRepositoryDirectory(".github");
            IReadOnlyList<RepositoryContent> rootContents = new[] { directory };

            var content = CreateContent("CODEOWNERS", "devguy");
            IReadOnlyList<RepositoryContent> contents = new[] { content };

            var repository = CreateRepository("repo");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(rootContents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, rootContents[0].Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, rootContents[0].Name + "/" + contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_ReturnsTrueIfCodeownersFileExistsInDocsDirectoryAndHasAtleastOneEntryAndGithubDirectoryExists()
        {
            var docsDir = CreateRepositoryDirectory("docs");
            var githubDir = CreateRepositoryDirectory(".github");
            IReadOnlyList<RepositoryContent> rootContents = new[] { docsDir, githubDir };

            var content = CreateContent("CODEOWNERS", "devguy");
            IReadOnlyList<RepositoryContent> contents = new[] { content };

            var repository = CreateRepository("repo");
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, MainBranch).Returns(Task.FromResult(rootContents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, rootContents[0].Name, MainBranch).Returns(Task.FromResult((IReadOnlyList<RepositoryContent>)new List<RepositoryContent>()));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, rootContents[0].Name, MainBranch).Returns(Task.FromResult(contents));
            MockRepositoryContentClient.GetAllContentsByRef(Owner.Name, repository.Name, rootContents[0].Name + "/" + contents[0].Name, MainBranch).Returns(Task.FromResult(contents));

            var result = await _rule.IsValid(MockClient, repository);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task IsValid_FixIsNotNullAndDoesntThrow()
        {
            var repository = CreateRepository("repo");

            var result = await _rule.IsValid(MockClient, repository);
            Assert.NotNull(result.Fix);
            await result.Fix(MockClient, repository);
        }

        private RepositoryContent CreateRepositoryDirectory(string name)
        {
            return new RepositoryContent(name, null, null, 0, ContentType.Dir, null, null, null, null, null, null, null, null);
        }

        private RepositoryContent CreateContent(string name, string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var converted = Convert.ToBase64String(bytes);
            return new RepositoryContent(name, null, null, 0, ContentType.File, null, null, null, null, null, converted, null, null);
        }

        private Repository CreateRepository(string name)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, Owner, name, null, false, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false, 0);
        }
    }
}
