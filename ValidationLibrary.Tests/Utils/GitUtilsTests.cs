using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using ValidationLibrary.Utils;

namespace ValidationLibrary.Tests.Utils
{
    [TestFixture]
    public class GitUtilsTests
    {
        private GitUtils _gitUtils;

        private IGitHubClient _mockClient;
        private IRepositoryBranchesClient _mockRepositoryBranchesClient;

        [SetUp]
        public void SetUp()
        {
            var mockLogger = Substitute.For<ILogger<GitUtils>>();
            _gitUtils = new GitUtils(mockLogger);

            _mockClient = Substitute.For<IGitHubClient>();
            _mockRepositoryBranchesClient = Substitute.For<IRepositoryBranchesClient>();
            _mockClient.Repository.Branch.Returns(_mockRepositoryBranchesClient);
        }

        [Test]
        public async Task PullRequestHasLiveBranch_ReturnTrueIfBranchReferredInPullRequestHasSameSha()
        {
            var pullRequest = CreatePullRequest("reference", "shasha");
            var branch = CreateBranch("reference", "shasha");
            _mockRepositoryBranchesClient.Get(Arg.Any<string>(), Arg.Any<string>(), "reference").Returns(branch);

            var result = await _gitUtils.PullRequestHasLiveBranch(_mockClient, pullRequest);
            Assert.True(result);
        }

        private Branch CreateBranch(string reference, string sha)
        {
            var commit = CreateGitReference(reference, sha);
            return new Branch("name", commit, false);
        }

        private PullRequest CreatePullRequest(string reference, string sha)
        {
            var headReference = CreateGitReference(reference, sha);

            return new PullRequest(0, null, null, null, null, null, null, null, 666, ItemState.Closed, "title", null, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, headReference, null, null, null, null, false, null, null, null, null, 0, 0, 0, 0, 0, null, false, null, null, null);
        }

        private GitReference CreateGitReference(string reference, string sha)
        {
            return new GitReference(null, null, null, reference, sha, null, CreateRepository("test"));
        }

        private Repository CreateRepository(string name)
        {
            return new Repository(null, null, null, null, null, null, null, 0, null, CreateUser(), name, null, null, null, null, false, false, 0, 0, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, false, false, false, false, 0, 0, null, null, null, false);
        }

        private User CreateUser()
        {
            return new User(null, null, null, 0, null, DateTime.UtcNow, DateTime.UtcNow, 0, null, 0, 0, false, null, 0, 0, null, "protacon", "protacon", null, 0, null, 0, 0, 0, null, new RepositoryPermissions(), false, null, null);
        }
    }
}